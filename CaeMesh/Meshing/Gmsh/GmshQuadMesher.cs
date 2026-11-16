// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Turns the quads and triangles gmsh meshed free on a face into quads only by QuadCleanup, on the nodes gmsh placed
    /// on the edges of the face: the elements and nodes of the face are read from gmsh, with their parameters on the
    /// surface, the cleanup works in the frame of ThreeBlockMapping that is close to isometric over the boundary of the
    /// face, the inner nodes are evaluated on the surface and the elements of the face are replaced, turning the way gmsh
    /// turned its own. The result is kept only when it is better: fewer triangles than before - none, one when
    /// the elements around the boundary add up to an odd number, or the few the cleanup found no good way to join - no
    /// folded element, and no corner of a scaled Jacobian below SubmapFaceDetector.MinScaledJacobian or below the worst
    /// corner gmsh left, whichever is lower, measured in space against the normal of the surface. A face gmsh meshed with
    /// quads only is cleaned as well, for its valences and its corners, and kept only with no fold, no more irregular
    /// inner nodes and no corner below the worst one gmsh left. A face of a periodic surface closed along one seam or two
    /// - a cylinder, a cone, a torus, a surface of revolution or extrusion - is cut open along them first (CutAlongSeams):
    /// a node on a seam is then a fixed node once for each side of the seam, with the parameters of that side, and is
    /// written back as the one node gmsh placed; the result is not kept when an element would then use a node twice or
    /// two elements the same nodes. A face with a degenerate edge, whose parameters do not run evenly over it, or with a
    /// node on an edge or a point inside it is left as gmsh meshed it.
    /// </summary>
    public static class GmshQuadMesher
    {
        // Variables                                                                                                                
        private static readonly double TriangleScale = 2 / Math.Sqrt(3);
        private const double MaxDegenerateLength = 1E-9;
        private const double SeamTolerance = 1E-6;


        // Methods                                                                                                                  
        /// <summary>
        /// Converts the face of the gmsh surface. Returns whether its elements were replaced; numOfTriangles is the number
        /// of triangles the face keeps and reason, when it keeps any, why.
        /// </summary>
        public static bool Mesh(int surfaceId, out int numOfTriangles, out string reason)
        {
            numOfTriangles = 0;
            reason = null;
            // The elements gmsh meshed
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] elementNodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 2, surfaceId);
            List<IntPtr[]> gmshElements = new List<IntPtr[]>();
            for (int t = 0; t < elementTypes.Length; t++)
            {
                int n = elementTypes[t] == (int)GmshElementType.LinearTriangle ? 3 :
                        elementTypes[t] == (int)GmshElementType.LinearQuadrilateral ? 4 : 0;
                if (n == 0) return false;
                for (int e = 0; e < elementTags[t].Length; e++)
                {
                    IntPtr[] nodes = new IntPtr[n];
                    Array.Copy(elementNodeTags[t], e * n, nodes, 0, n);
                    gmshElements.Add(nodes);
                    if (n == 3) numOfTriangles++;
                }
            }
            bool triangleFree = numOfTriangles == 0;
            // The nodes on the edges of the face, which stay; a seam is named twice around the face
            int[] edgeIds;
            Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
            List<int> uniqueEdgeIds = new List<int>();
            List<int> seamEdgeIds = new List<int>();
            foreach (var signedEdgeId in edgeIds)
            {
                int edgeId = Math.Abs(signedEdgeId);
                if (!uniqueEdgeIds.Contains(edgeId)) uniqueEdgeIds.Add(edgeId);
                else if (!seamEdgeIds.Contains(edgeId)) seamEdgeIds.Add(edgeId);
                else
                {
                    reason = "Edge " + edgeId + " runs more than twice around it";
                    return false;
                }
            }
            double faceSize = GetFaceSize(surfaceId);
            foreach (var edgeId in uniqueEdgeIds)
            {
                if (IsDegenerateEdge(edgeId, faceSize))
                {
                    reason = "Edge " + edgeId + " of it is degenerate";
                    return false;
                }
            }
            HashSet<IntPtr> boundaryTags = new HashSet<IntPtr>();
            HashSet<IntPtr> rimTags = new HashSet<IntPtr>();
            IntPtr[] tags;
            double[] coor;
            foreach (var edgeId in uniqueEdgeIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, true, false);
                foreach (var tag in tags)
                {
                    boundaryTags.Add(tag);
                    if (!seamEdgeIds.Contains(edgeId)) rimTags.Add(tag);
                }
            }
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, false, false);
            HashSet<IntPtr> innerTags = new HashSet<IntPtr>(tags);
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
            Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < tags.Length; i++)
                tagXyz[tags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            // The boundary nodes first and then the inner ones, each in the order the elements meet them
            List<IntPtr> nodeTags = new List<IntPtr>();
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var element in gmshElements)
                {
                    foreach (var tag in element)
                    {
                        if (tagIndex.ContainsKey(tag)) continue;
                        bool isBoundary = boundaryTags.Contains(tag);
                        if (!isBoundary && !innerTags.Contains(tag))
                        {
                            reason = "it carries an edge or a point inside it";
                            return false;
                        }
                        if (isBoundary != (pass == 0)) continue;
                        tagIndex.Add(tag, nodeTags.Count);
                        nodeTags.Add(tag);
                    }
                }
                if (pass == 0 && nodeTags.Count == 0) return false;
            }
            int numOfNodes = nodeTags.Count;
            int numOfBoundaryNodes = 0;
            while (numOfBoundaryNodes < numOfNodes && boundaryTags.Contains(nodeTags[numOfBoundaryNodes])) numOfBoundaryNodes++;
            double[][] xyz = new double[numOfNodes][];
            double[] flatXyz = new double[3 * numOfNodes];
            for (int i = 0; i < numOfNodes; i++)
            {
                if (!tagXyz.TryGetValue(nodeTags[i], out xyz[i])) return false;
                for (int d = 0; d < 3; d++) flatXyz[3 * i + d] = xyz[i][d];
            }
            int[][] elements = new int[gmshElements.Count][];
            for (int e = 0; e < elements.Length; e++)
            {
                elements[e] = new int[gmshElements[e].Length];
                for (int c = 0; c < elements[e].Length; c++) elements[e][c] = tagIndex[gmshElements[e][c]];
            }
            // The parameters, in the frame fitted over the segments of the boundary
            double[] flatUv;
            Gmsh.Model.GetParametrization(2, surfaceId, flatXyz, out flatUv);
            if (flatUv.Length != 2 * numOfNodes)
            {
                reason = "its nodes could not be placed on its surface";
                return false;
            }
            double[][] uv = new double[numOfNodes][];
            for (int i = 0; i < numOfNodes; i++) uv[i] = new double[] { flatUv[2 * i], flatUv[2 * i + 1] };
            int numOfBoundarySegments = GetBoundarySegments(elements).Count;
            // A face closed along seams is cut open along them: a node on a seam appears once for every side of it the
            // elements around it lie on, with the parameters of that side, so that the face becomes a plane region
            bool cut = seamEdgeIds.Count > 0;
            if (cut)
            {
                List<int> cutNodeIds;
                double[][] cutUv;
                int[][] cutElements;
                int numOfCutBoundaryNodes;
                if (!CutAlongSeams(surfaceId, seamEdgeIds, tagIndex, numOfBoundaryNodes, xyz, uv, elements,
                                   out cutNodeIds, out cutUv, out cutElements, out numOfCutBoundaryNodes, out reason))
                    return false;
                numOfNodes = cutNodeIds.Count;
                numOfBoundaryNodes = numOfCutBoundaryNodes;
                nodeTags = cutNodeIds.Select(id => nodeTags[id]).ToList();
                xyz = cutNodeIds.Select(id => xyz[id]).ToArray();
                uv = cutUv;
                elements = cutElements;
            }
            List<int[]> segments = GetBoundarySegments(elements);
            double[][][] segmentUv = new double[segments.Count][][];
            double[][][] segmentXyz = new double[segments.Count][][];
            for (int s = 0; s < segments.Count; s++)
            {
                segmentUv[s] = new double[][] { uv[segments[s][0]], uv[segments[s][1]] };
                segmentXyz[s] = new double[][] { xyz[segments[s][0]], xyz[segments[s][1]] };
            }
            double[] w = GetFrame(segmentUv, segmentXyz);
            double[][] points = new double[numOfNodes][];
            for (int i = 0; i < numOfNodes; i++) points[i] = ThreeBlockMapping.ToFrame(w, uv[i]);
            if (!AreAreasOfOneSign(points, elements))
            {
                reason = "its parameters do not run evenly over it";
                return false;
            }
            bool oddTotal = numOfBoundarySegments % 2 == 1;
            string oddReason = "the elements around its boundary add up to an odd number, " + numOfBoundarySegments;
            string notJoinedReason = "the cleanup found no good way to join them into quads";
            //
            bool[] fixedNodes = new bool[numOfNodes];
            for (int i = 0; i < numOfBoundaryNodes; i++) fixedNodes[i] = true;
            QuadCleanupResult result;
            try
            {
                result = QuadCleanup.Clean(points, elements, fixedNodes);
            }
            catch (CaeException ex)
            {
                reason = ex.Message.Trim().TrimEnd('.');
                if (reason.StartsWith("The ")) reason = "the " + reason.Substring(4);
                return false;
            }
            // A face of quads only is kept as gmsh meshed it unless the cleanup leaves no more irregular inner nodes; the
            // nodes on a seam, inner nodes of the face, are counted once it is joined again
            if (triangleFree && (result.NumberOfFoldedElements > 0 ||
                                 !cut && result.NumberOfIrregularNodes > result.NumberOfIrregularNodesBefore)) return false;
            if (!triangleFree && result.NumberOfTriangles >= numOfTriangles)
            {
                reason = oddTotal && numOfTriangles == 1 ? oddReason : notJoinedReason;
                return false;
            }
            if (result.NumberOfFoldedElements > 0)
            {
                reason = result.NumberOfFoldedElements + " of the elements the cleanup made fold";
                return false;
            }
            // The inner nodes the cleanup kept, on the surface
            int[] newIds = new int[result.Points.Length];
            List<double[]> innerUv = new List<double[]>();
            for (int i = 0; i < newIds.Length; i++)
            {
                if (i < numOfBoundaryNodes) newIds[i] = i;
                else if (result.IsNodeUsed[i])
                {
                    newIds[i] = numOfBoundaryNodes + innerUv.Count;
                    innerUv.Add(ThreeBlockMapping.FromFrame(w, result.Points[i]));
                }
                else newIds[i] = -1;
            }
            double[] innerFlatUv = new double[2 * innerUv.Count];
            for (int i = 0; i < innerUv.Count; i++)
            {
                innerFlatUv[2 * i] = innerUv[i][0];
                innerFlatUv[2 * i + 1] = innerUv[i][1];
            }
            double[] innerFlatXyz = new double[0];
            if (innerUv.Count > 0) Gmsh.Model.GetValue(2, surfaceId, innerFlatUv, out innerFlatXyz);
            if (innerFlatXyz == null || innerFlatXyz.Length != 3 * innerUv.Count)
            {
                reason = "its inner nodes could not be placed on its surface";
                return false;
            }
            double[][] innerXyz = new double[innerUv.Count][];
            for (int i = 0; i < innerXyz.Length; i++)
                innerXyz[i] = new double[] { innerFlatXyz[3 * i], innerFlatXyz[3 * i + 1], innerFlatXyz[3 * i + 2] };
            int[][] newElements = new int[result.Elements.Length][];
            for (int e = 0; e < newElements.Length; e++)
            {
                newElements[e] = new int[result.Elements[e].Length];
                for (int c = 0; c < newElements[e].Length; c++) newElements[e][c] = newIds[result.Elements[e][c]];
            }
            if (cut)
            {
                reason = GetSeamJoinReason(newElements, nodeTags, numOfBoundaryNodes);
                if (reason != null) return false;
                if (triangleFree && CountIrregularInnerNodes(GetNodeKeys(newElements, nodeTags, numOfBoundaryNodes), rimTags) >
                                    CountIrregularInnerNodes(gmshElements.Select(element => element.Select(tag => tag.ToInt64())
                                                                                                   .ToArray()), rimTags))
                    return false;
            }
            // The quality in space, against the normal of the surface, turned the way gmsh's elements turn
            double[][] newXyz = new double[numOfBoundaryNodes + innerXyz.Length][];
            double[][] newUv = new double[newXyz.Length][];
            for (int i = 0; i < numOfBoundaryNodes; i++)
            {
                newXyz[i] = xyz[i];
                newUv[i] = uv[i];
            }
            for (int i = 0; i < innerXyz.Length; i++)
            {
                newXyz[numOfBoundaryNodes + i] = innerXyz[i];
                newUv[numOfBoundaryNodes + i] = innerUv[i];
            }
            double orientation = GetSpaceOrientation(surfaceId, elements, xyz, uv);
            double minScaledJacobian = GetSpaceMinScaledJacobian(surfaceId, newElements, newXyz, newUv, orientation);
            double gmshMinScaledJacobian = GetSpaceMinScaledJacobian(surfaceId, elements, xyz, uv, orientation);
            double floor = triangleFree ? gmshMinScaledJacobian :
                           Math.Min(SubmapFaceDetector.MinScaledJacobian, gmshMinScaledJacobian);
            if (triangleFree && minScaledJacobian < floor) return false;
            if (minScaledJacobian < floor)
            {
                reason = "the cleaned mesh would have a corner of the scaled Jacobian " + Math.Round(minScaledJacobian, 3) +
                         ", below " + Math.Round(floor, 3);
                return false;
            }
            IntPtr[] boundaryNodeTags = new IntPtr[numOfBoundaryNodes];
            for (int i = 0; i < numOfBoundaryNodes; i++) boundaryNodeTags[i] = nodeTags[i];
            GmshThreeBlockMesher.ReplaceSurfaceElements(surfaceId, boundaryNodeTags, newElements, innerUv.ToArray(),
                                                        innerXyz, false, true);
            numOfTriangles = result.NumberOfTriangles;
            reason = numOfTriangles == 0 ? null : oddTotal && numOfTriangles == 1 ? oddReason : notJoinedReason;
            return true;
        }
        /// <summary>
        /// The edges of a face whose numbers of elements add up to the parity of the elements around it, from the edges
        /// of its loops: every edge once but a seam, which the face meets on both sides and which never changes the
        /// parity. Null for a face the conversion does not take: with a degenerate edge, or with an edge named twice that
        /// is not a seam.
        /// </summary>
        public static int[] GetParityEdgeIds(int surfaceId, int[] loopEdgeIds)
        {
            List<int> singleEdgeIds = new List<int>();
            List<int> seamEdgeIds = new List<int>();
            foreach (var edgeId in loopEdgeIds)
            {
                if (seamEdgeIds.Contains(edgeId)) return null;
                if (singleEdgeIds.Remove(edgeId)) seamEdgeIds.Add(edgeId);
                else singleEdgeIds.Add(edgeId);
            }
            double faceSize = GetFaceSize(surfaceId);
            foreach (var edgeId in singleEdgeIds.Concat(seamEdgeIds))
            {
                if (IsDegenerateEdge(edgeId, faceSize)) return null;
            }
            int direction;
            double period;
            foreach (var edgeId in seamEdgeIds)
            {
                if (!GetSeamPeriod(surfaceId, edgeId, out direction, out period)) return null;
            }
            return singleEdgeIds.ToArray();
        }
        private static double GetFaceSize(int surfaceId)
        {
            double area;
            Gmsh.Model.OCC.GetMass(2, surfaceId, out area);
            return area > 0 ? Math.Sqrt(area) : 0;
        }
        private static bool IsDegenerateEdge(int edgeId, double faceSize)
        {
            double length;
            Gmsh.Model.OCC.GetMass(1, edgeId, out length);
            return !(length > MaxDegenerateLength * faceSize);
        }
        /// <summary>
        /// Whether the edge named twice around the face is a seam that closes it along one of its parameters, which then
        /// changes by the period from one side of the seam to the other.
        /// </summary>
        internal static bool GetSeamPeriod(int surfaceId, int edgeId, out int direction, out double period)
        {
            direction = -1;
            period = 0;
            try
            {
                double[] min;
                double[] max;
                Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                if (min.Length != 1 || max.Length != 1) return false;
                double[] middle = new double[] { 0.5 * (min[0] + max[0]) };
                double[] first;
                double[] second;
                Gmsh.Model.ReparametrizeOnSurface(1, edgeId, middle, surfaceId, out first, 0);
                Gmsh.Model.ReparametrizeOnSurface(1, edgeId, middle, surfaceId, out second, 1);
                if (first.Length != 2 || second.Length != 2) return false;
                double[] difference = new double[] { Math.Abs(second[0] - first[0]), Math.Abs(second[1] - first[1]) };
                direction = difference[0] >= difference[1] ? 0 : 1;
                period = difference[direction];
                Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
                if (min.Length != 2 || max.Length != 2) return false;
                double range = max[direction] - min[direction];
                return period > 0 && difference[1 - direction] <= SeamTolerance * period &&
                       Math.Abs(range - period) <= SeamTolerance * period;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return false;
            }
        }
        /// <summary>
        /// Cuts the mesh of a face open along its seams. Every element takes, at a node on a seam, the parameters of the
        /// side it lies on: the period is added to or taken from the parameter the seam closes so that it comes nearest
        /// to that parameter of its corners off the seam, or of its centre where all its corners are on the seam. The
        /// parameters of the other nodes are first brought within the bounds of the face. The copies of the boundary
        /// nodes come first and then the inner nodes, each in the order the elements meet them.
        /// </summary>
        private static bool CutAlongSeams(int surfaceId, List<int> seamEdgeIds, Dictionary<IntPtr, int> tagIndex,
                                          int numOfBoundaryNodes, double[][] xyz, double[][] uv, int[][] elements,
                                          out List<int> cutNodeIds, out double[][] cutUv, out int[][] cutElements,
                                          out int numOfCutBoundaryNodes, out string reason)
        {
            cutNodeIds = new List<int>();
            cutUv = null;
            cutElements = null;
            numOfCutBoundaryNodes = 0;
            reason = null;
            int numOfNodes = uv.Length;
            double[] period = new double[2];
            int[] seamDirections = new int[numOfNodes];
            IntPtr[] tags;
            double[] coor;
            foreach (var edgeId in seamEdgeIds)
            {
                int direction;
                double edgePeriod;
                if (!GetSeamPeriod(surfaceId, edgeId, out direction, out edgePeriod))
                {
                    reason = "Edge " + edgeId + " runs twice around it but is not a seam of its surface";
                    return false;
                }
                period[direction] = edgePeriod;
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, true, false);
                int id;
                foreach (var tag in tags)
                {
                    if (tagIndex.TryGetValue(tag, out id)) seamDirections[id] |= 1 << direction;
                }
            }
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            for (int i = 0; i < numOfNodes; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (period[k] > 0 && (seamDirections[i] & (1 << k)) == 0) uv[i][k] = Wrap(uv[i][k], min[k], period[k]);
                }
            }
            // The side of every corner on a seam, coded as 3 (shift of u + 1) + shift of v + 1
            int[][] cornerShifts = new int[elements.Length][];
            for (int e = 0; e < elements.Length; e++)
            {
                int[] element = elements[e];
                cornerShifts[e] = new int[element.Length];
                for (int c = 0; c < element.Length; c++) cornerShifts[e][c] = 4;
                double[] centre = null;
                for (int k = 0; k < 2; k++)
                {
                    if (period[k] == 0 || !element.Any(node => (seamDirections[node] & (1 << k)) != 0)) continue;
                    double reference = 0;
                    int count = 0;
                    foreach (var node in element)
                    {
                        if ((seamDirections[node] & (1 << k)) != 0) continue;
                        reference += uv[node][k];
                        count++;
                    }
                    if (count > 0) reference /= count;
                    else
                    {
                        if (centre == null) centre = GetCentreParameters(surfaceId, element, xyz, min, period);
                        if (centre == null)
                        {
                            reason = "the side of the seam an element of it lies on could not be found";
                            return false;
                        }
                        reference = centre[k];
                    }
                    for (int c = 0; c < element.Length; c++)
                    {
                        if ((seamDirections[element[c]] & (1 << k)) == 0) continue;
                        int shift = (int)Math.Round((reference - uv[element[c]][k]) / period[k]);
                        if (Math.Abs(shift) > 1)
                        {
                            reason = "the side of the seam an element of it lies on could not be found";
                            return false;
                        }
                        cornerShifts[e][c] += k == 0 ? 3 * shift : shift;
                    }
                }
            }
            Dictionary<long, int> cutIndex = new Dictionary<long, int>();
            List<double[]> cutUvList = new List<double[]>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int e = 0; e < elements.Length; e++)
                {
                    for (int c = 0; c < elements[e].Length; c++)
                    {
                        int node = elements[e][c];
                        if ((node < numOfBoundaryNodes) != (pass == 0)) continue;
                        int code = cornerShifts[e][c];
                        long key = 9L * node + code;
                        if (cutIndex.ContainsKey(key)) continue;
                        cutIndex.Add(key, cutNodeIds.Count);
                        cutNodeIds.Add(node);
                        cutUvList.Add(new double[] { uv[node][0] + (code / 3 - 1) * period[0],
                                                     uv[node][1] + (code % 3 - 1) * period[1] });
                    }
                }
                if (pass == 0) numOfCutBoundaryNodes = cutNodeIds.Count;
            }
            cutUv = cutUvList.ToArray();
            cutElements = new int[elements.Length][];
            for (int e = 0; e < elements.Length; e++)
            {
                cutElements[e] = new int[elements[e].Length];
                for (int c = 0; c < elements[e].Length; c++)
                    cutElements[e][c] = cutIndex[9L * elements[e][c] + cornerShifts[e][c]];
            }
            return true;
        }
        /// <summary>
        /// The parameters of the centre of the element on the surface, within the bounds of the face along the closed
        /// parameters; null when it could not be placed.
        /// </summary>
        private static double[] GetCentreParameters(int surfaceId, int[] element, double[][] xyz, double[] min,
                                                    double[] period)
        {
            double[] centre = new double[3];
            foreach (var node in element)
            {
                for (int d = 0; d < 3; d++) centre[d] += xyz[node][d] / element.Length;
            }
            double[] parameters;
            Gmsh.Model.GetParametrization(2, surfaceId, centre, out parameters);
            if (parameters == null || parameters.Length != 2) return null;
            for (int k = 0; k < 2; k++)
            {
                if (period[k] > 0) parameters[k] = Wrap(parameters[k], min[k], period[k]);
            }
            return parameters;
        }
        private static double Wrap(double value, double start, double period)
        {
            double wrapped = (value - start) % period;
            if (wrapped < 0) wrapped += period;
            return start + wrapped;
        }
        /// <summary>
        /// Why the elements of a face cut along its seams cannot be written with each node on a seam once: an element
        /// that would use a node twice, or two elements of the same nodes; null when they can.
        /// </summary>
        private static string GetSeamJoinReason(int[][] elements, List<IntPtr> nodeTags, int numOfBoundaryNodes)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var element in GetNodeKeys(elements, nodeTags, numOfBoundaryNodes))
            {
                long[] nodes = (long[])element.Clone();
                Array.Sort(nodes);
                for (int c = 1; c < nodes.Length; c++)
                {
                    if (nodes[c] == nodes[c - 1]) return "the cleanup would join the two sides of a seam in one element";
                }
                if (!keys.Add(string.Join(" ", nodes))) return "the cleanup would make two elements of the same nodes";
            }
            return null;
        }
        /// <summary>
        /// The elements by the gmsh tags of their boundary nodes, every copy of a node on a seam by the one tag, and by
        /// negative keys for the inner nodes.
        /// </summary>
        private static IEnumerable<long[]> GetNodeKeys(int[][] elements, List<IntPtr> nodeTags, int numOfBoundaryNodes)
        {
            return elements.Select(element => element.Select(id => id < numOfBoundaryNodes ? nodeTags[id].ToInt64() : -1L - id)
                                                     .ToArray());
        }
        /// <summary>
        /// The nodes not on an edge around the face that are not shared by four elements.
        /// </summary>
        private static int CountIrregularInnerNodes(IEnumerable<long[]> elements, HashSet<IntPtr> rimTags)
        {
            Dictionary<long, int> valences = new Dictionary<long, int>();
            foreach (var element in elements)
            {
                foreach (var node in element)
                {
                    int valence;
                    valences.TryGetValue(node, out valence);
                    valences[node] = valence + 1;
                }
            }
            return valences.Count(entry => entry.Value != 4 && !rimTags.Contains(new IntPtr(entry.Key)));
        }
        /// <summary>
        /// The frame of ThreeBlockMapping.GetMetricFrame fitted over the boundary segments. When every segment runs along
        /// one parameter, as around a face cut along its seam, the fit leaves the shear open and gives the identity; the
        /// scales of the two parameters are then fitted alone.
        /// </summary>
        internal static double[] GetFrame(double[][][] segmentUv, double[][][] segmentXyz)
        {
            double[] w = ThreeBlockMapping.GetMetricFrame(segmentUv, segmentXyz);
            if (w[0] != 1 || w[1] != 0 || w[2] != 1) return w;
            double uu = 0;
            double uv = 0;
            double vv = 0;
            double uLength = 0;
            double vLength = 0;
            for (int s = 0; s < segmentUv.Length; s++)
            {
                double du = segmentUv[s][1][0] - segmentUv[s][0][0];
                double dv = segmentUv[s][1][1] - segmentUv[s][0][1];
                double lengthSquared = 0;
                for (int d = 0; d < 3; d++)
                    lengthSquared += (segmentXyz[s][1][d] - segmentXyz[s][0][d]) * (segmentXyz[s][1][d] - segmentXyz[s][0][d]);
                uu += du * du * du * du;
                uv += du * du * dv * dv;
                vv += dv * dv * dv * dv;
                uLength += du * du * lengthSquared;
                vLength += dv * dv * lengthSquared;
            }
            double determinant = uu * vv - uv * uv;
            if (!(determinant > 1E-12 * uu * vv)) return w;
            double gu = (uLength * vv - uv * vLength) / determinant;
            double gv = (uu * vLength - uv * uLength) / determinant;
            if (!(gu > 0) || !(gv > 0)) return w;
            return new double[] { Math.Sqrt(gu), 0, Math.Sqrt(gv) };
        }
        /// <summary>
        /// The edges of the mesh that only one element has, in the order of the elements.
        /// </summary>
        private static List<int[]> GetBoundarySegments(int[][] elements)
        {
            Dictionary<long, int> edgeCount = new Dictionary<long, int>();
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                {
                    long key = GetEdgeKey(element[c], element[(c + 1) % element.Length]);
                    int count;
                    edgeCount.TryGetValue(key, out count);
                    edgeCount[key] = count + 1;
                }
            }
            List<int[]> segments = new List<int[]>();
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                {
                    int a = element[c];
                    int b = element[(c + 1) % element.Length];
                    if (edgeCount[GetEdgeKey(a, b)] == 1) segments.Add(new int[] { a, b });
                }
            }
            return segments;
        }
        private static long GetEdgeKey(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }
        private static bool AreAreasOfOneSign(double[][] points, int[][] elements)
        {
            int numOfPositive = 0;
            int numOfNegative = 0;
            foreach (var element in elements)
            {
                double area = 0;
                for (int c = 0; c < element.Length; c++)
                {
                    double[] p = points[element[c]];
                    double[] q = points[element[(c + 1) % element.Length]];
                    area += p[0] * q[1] - q[0] * p[1];
                }
                if (area > 0) numOfPositive++;
                else if (area < 0) numOfNegative++;
                else return false;
            }
            return numOfPositive == 0 || numOfNegative == 0;
        }
        /// <summary>
        /// 1 when the elements gmsh meshed turn with the normal of the surface, -1 when against it.
        /// </summary>
        private static double GetSpaceOrientation(int surfaceId, int[][] elements, double[][] xyz, double[][] uv)
        {
            double[][] normals = GetNormals(surfaceId, uv);
            double sum = 0;
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++) sum += GetSpaceScaledJacobian(element, c, xyz, normals, 1);
            }
            return sum < 0 ? -1 : 1;
        }
        private static double GetSpaceMinScaledJacobian(int surfaceId, int[][] elements, double[][] xyz, double[][] uv,
                                                        double orientation)
        {
            double[][] normals = GetNormals(surfaceId, uv);
            double min = 1;
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                    min = Math.Min(min, GetSpaceScaledJacobian(element, c, xyz, normals, orientation));
            }
            return min;
        }
        internal static double[][] GetNormals(int surfaceId, double[][] uv)
        {
            double[] flatUv = new double[2 * uv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                flatUv[2 * i] = uv[i][0];
                flatUv[2 * i + 1] = uv[i][1];
            }
            double[] flatNormals;
            Gmsh.Model.GetNormal(surfaceId, flatUv, out flatNormals);
            if (flatNormals == null || flatNormals.Length != 3 * uv.Length)
                throw new CaeException("The normals of Surface " + surfaceId + " could not be evaluated.");
            double[][] normals = new double[uv.Length][];
            for (int i = 0; i < uv.Length; i++)
                normals[i] = new double[] { flatNormals[3 * i], flatNormals[3 * i + 1], flatNormals[3 * i + 2] };
            return normals;
        }
        /// <summary>
        /// The sine of the angle at corner c of the element in space, negative where the corner turns against the normal
        /// of the surface at its node times the orientation; a triangle scaled so that 60 degrees count as 1.
        /// </summary>
        internal static double GetSpaceScaledJacobian(int[] element, int c, double[][] xyz, double[][] normals,
                                                      double orientation)
        {
            int n = element.Length;
            double[] p = xyz[element[c]];
            double[] next = xyz[element[(c + 1) % n]];
            double[] previous = xyz[element[(c + n - 1) % n]];
            double[] a = new double[] { next[0] - p[0], next[1] - p[1], next[2] - p[2] };
            double[] b = new double[] { previous[0] - p[0], previous[1] - p[1], previous[2] - p[2] };
            double[] normal = normals[element[c]];
            double cross = (a[1] * b[2] - a[2] * b[1]) * normal[0] + (a[2] * b[0] - a[0] * b[2]) * normal[1] +
                           (a[0] * b[1] - a[1] * b[0]) * normal[2];
            double lengths = Math.Sqrt((a[0] * a[0] + a[1] * a[1] + a[2] * a[2]) * (b[0] * b[0] + b[1] * b[1] + b[2] * b[2]));
            if (!(lengths > 0)) return -1;
            return orientation * cross / lengths * (n == 3 ? TriangleScale : 1);
        }
    }
}
