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
    /// Turns the quads and triangles gmsh meshed free on a face into quads only by QuadCleanup, on the nodes gmsh
    /// placed on the edges of the face: the elements and nodes of the face are read from gmsh, with their parameters on
    /// the surface, the cleanup works in the frame of ThreeBlockMapping that is close to isometric over the boundary of
    /// the face, the inner nodes are evaluated on the surface and the elements of the face are replaced, turning the
    /// way gmsh turned its own. The result is kept only when it is better: fewer triangles than before - none, one when
    /// the elements around the boundary add up to an odd number, or the few the cleanup found no good way to join - no
    /// folded element, and no corner of a scaled Jacobian below SubmapFaceDetector.MinScaledJacobian, below what the
    /// sharpest corner of the boundary allows (half its sine, for a corner below 90 degrees in space) or below the
    /// worst corner gmsh left, whichever is lowest, measured in space against the normal of the surface. Parameters
    /// that fold gmsh's own mesh in the frame, as those of a sliver of a blend that squeeze its corner, give way to the
    /// projection on the mean plane of the face when that folds none of its elements; the inner nodes are then placed
    /// at the points of the surface nearest to the plane. A face gmsh meshed with quads only is cleaned as well, for
    /// its valences and its corners, and kept only with no fold, no more irregular inner nodes and no corner below the
    /// worst one gmsh left. A face of a periodic surface closed along one seam or two - a cylinder, a cone, a torus, a
    /// surface of revolution or extrusion - is cut open along them first (CutAlongSeams): a node on a seam is then a
    /// fixed node once for each side of the seam, with the parameters of that side, and is written back as the one node
    /// gmsh placed; the result is not kept when an element would then use a node twice or two elements the same nodes.
    /// A face that carries curves or points inside it is cut open along the curves (CutAlongEmbeddedEdges) the same
    /// way, its nodes on them fixed, so that the cleanup neither moves nor crosses them. A face with a degenerate edge,
    /// a pole, that gmsh left triangles on is cleaned in the projection on its mean plane, or in the conformal chart
    /// round the pole (GmshPoleChart) when the projection folds an element, a seam of it then being a curve inside it,
    /// and its corners at the pole measured against the mean normal of its elements there; it is left as gmsh meshed it
    /// when both fold, and so is one gmsh meshed with quads only. Such a face has no even total in the balance. A
    /// cleaned mesh of the last passes of the cleanup, which add nodes, is kept only when its worst element shape in
    /// space is not below 0.2 or below gmsh's, whichever is lower. A face whose parameters do not run evenly over it,
    /// or with a curve inside it and a seam, is left as gmsh meshed it.
    /// </summary>
    public static class GmshQuadMesher
    {
        // Variables                                                                                                                
        private static readonly double TriangleScale = 2 / Math.Sqrt(3);
        private const double MaxDegenerateLength = 1E-9;
        private const double SeamTolerance = 1E-6;
        private const double SharpCornerShare = 0.5;
        private const double ShapeFloor = 0.2;
        internal const string OddTotalReason = "the elements around its boundary add up to an odd number";


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
            // A degenerate edge, a pole, is one node of the boundary in space but a line in the parameters, so such a
            // face is cleaned in the projection on its mean plane, where a seam is no cut but a curve inside the face
            double faceSize = GetFaceSize(surfaceId);
            int degenerateEdgeId = uniqueEdgeIds.FirstOrDefault(edgeId => IsDegenerateEdge(edgeId, faceSize));
            bool poled = degenerateEdgeId != 0;
            if (poled && triangleFree) return false;
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
            // The nodes on an edge or a point inside the face stay as well
            HashSet<IntPtr> fixedTags = new HashSet<IntPtr>(boundaryTags);
            foreach (var element in gmshElements)
            {
                foreach (var tag in element)
                {
                    if (!boundaryTags.Contains(tag) && !innerTags.Contains(tag)) fixedTags.Add(tag);
                }
            }
            bool embedded = fixedTags.Count > boundaryTags.Count || poled && seamEdgeIds.Count > 0;
            if (embedded && !poled && seamEdgeIds.Count > 0)
            {
                reason = "it carries an edge or a point inside it and is closed along a seam";
                return false;
            }
            // The fixed nodes first and then the inner ones, each in the order the elements meet them
            List<IntPtr> nodeTags = new List<IntPtr>();
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (var element in gmshElements)
                {
                    foreach (var tag in element)
                    {
                        if (tagIndex.ContainsKey(tag)) continue;
                        if (fixedTags.Contains(tag) != (pass == 0)) continue;
                        tagIndex.Add(tag, nodeTags.Count);
                        nodeTags.Add(tag);
                    }
                }
                if (pass == 0 && nodeTags.Count == 0) return false;
            }
            if (embedded)
            {
                // The nodes on curves and points inside the face are not among the nodes of the surface
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, -1, -1, false, false);
                for (int i = 0; i < tags.Length; i++)
                {
                    if (fixedTags.Contains(tags[i]) && !tagXyz.ContainsKey(tags[i]))
                        tagXyz[tags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
                }
            }
            int numOfNodes = nodeTags.Count;
            int numOfBoundaryNodes = 0;
            while (numOfBoundaryNodes < numOfNodes && fixedTags.Contains(nodeTags[numOfBoundaryNodes]))
                numOfBoundaryNodes++;
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
            // A face with curves inside it is cut open along them, each side of a curve a boundary of its own
            if (embedded)
            {
                HashSet<long> embeddedEdges = GetEmbeddedEdges(uniqueEdgeIds, elements, tagIndex);
                if (poled) embeddedEdges.UnionWith(GetCurveEdges(seamEdgeIds, tagIndex));
                CutAlongEmbeddedEdges(embeddedEdges, ref numOfBoundaryNodes, nodeTags, ref xyz, ref uv, elements);
                numOfNodes = nodeTags.Count;
            }
            // A face closed along seams is cut open along them: a node on a seam appears once for every side of it the
            // elements around it lie on, with the parameters of that side, so that the face becomes a plane region
            bool cut = seamEdgeIds.Count > 0 && !poled;
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
            // Parameters that fold gmsh's own mesh, as those of a sliver of a blend whose corner they squeeze, give way
            // to the projection on the mean plane of the face when that folds none of its elements
            double[][] plane = null;
            if (!cut && (poled || CountFoldedElements(points, elements) > 0))
            {
                double[][] projected = GetProjectedPoints(xyz, elements, out plane);
                if (projected != null && CountFoldedElements(projected, elements) == 0) points = projected;
                else plane = null;
            }
            // A pole face the projection folds, a cap reaching beyond its equator, takes the conformal chart round the
            // pole
            GmshPoleChart chart = null;
            if (poled && plane == null)
            {
                chart = GetPoleChart(surfaceId, degenerateEdgeId, nodeTags, xyz, uv, numOfBoundaryNodes, faceSize);
                double[][] charted = chart == null ? null : uv.Select(chart.ToChart).ToArray();
                if (charted != null && CountFoldedElements(charted, elements) == 0) points = charted;
                else
                {
                    reason = "Edge " + degenerateEdgeId + " of it is degenerate and neither its projection on its " +
                             "mean plane nor its chart round the pole keep its elements unfolded";
                    return false;
                }
            }
            if (!AreAreasOfOneSign(points, elements))
            {
                reason = "its parameters do not run evenly over it";
                return false;
            }
            bool oddTotal = numOfBoundarySegments % 2 == 1;
            string oddReason = OddTotalReason + ", " + numOfBoundarySegments;
            if (poled) oddReason += ", which the balance does not make even around a face with a degenerate edge";
            //
            bool[] fixedNodes = new bool[numOfNodes];
            for (int i = 0; i < numOfBoundaryNodes; i++) fixedNodes[i] = true;
            // The pole is no corner of the face, whatever angle its edges make in space
            HashSet<int> poleNodes = new HashSet<int>();
            if (poled)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, degenerateEdgeId, true, false);
                HashSet<IntPtr> poleTags = new HashSet<IntPtr>(tags);
                for (int i = 0; i < numOfBoundaryNodes; i++)
                {
                    if (poleTags.Contains(nodeTags[i])) poleNodes.Add(i);
                }
            }
            // A cleaned mesh that is not kept in space is tried again in the projection on the mean plane of the face,
            // and last as the cleanup made it before its last passes, which keeps the triangles they would have joined
            double[][] framePoints = points;
            double[][] framePlane = plane;
            string firstReason = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0 && triangleFree) break;
                if (attempt == 1)
                {
                    if (cut || framePlane != null || chart != null) continue;
                    points = GetProjectedPoints(xyz, elements, out plane);
                    if (points == null || CountFoldedElements(points, elements) > 0 ||
                        !AreAreasOfOneSign(points, elements))
                        continue;
                }
                else if (attempt == 2)
                {
                    points = framePoints;
                    plane = framePlane;
                }
                if (TryClean(surfaceId, points, plane, chart, w, attempt < 2, fixedNodes, elements, xyz, uv, nodeTags,
                             numOfBoundaryNodes, segments, cut, embedded, triangleFree, numOfTriangles, rimTags,
                             gmshElements, poleNodes, oddTotal ? oddReason : null, out numOfTriangles, out reason))
                    return true;
                if (firstReason == null) firstReason = reason ?? "";
            }
            reason = string.IsNullOrEmpty(firstReason) ? null : firstReason;
            return false;
        }
        /// <summary>
        /// Cleans the elements of the face given in the plane frame, in the projection plane when plane is not null,
        /// with the last passes of the cleanup or without them, and replaces the elements of the face when the result
        /// is kept. Returns whether it was kept; numOfTriangles is then the number of triangles the face keeps, and
        /// reason why.
        /// </summary>
        private static bool TryClean(int surfaceId, double[][] points, double[][] plane, GmshPoleChart chart,
                                     double[] w, bool lastPasses, bool[] fixedNodes, int[][] elements, double[][] xyz,
                                     double[][] uv, List<IntPtr> nodeTags, int numOfBoundaryNodes, List<int[]> segments,
                                     bool cut, bool embedded, bool triangleFree, int numOfGmshTriangles,
                                     HashSet<IntPtr> rimTags, List<IntPtr[]> gmshElements, HashSet<int> poleNodes,
                                     string oddReason, out int numOfTriangles, out string reason)
        {
            numOfTriangles = numOfGmshTriangles;
            reason = null;
            string notJoinedReason = "the cleanup found no good way to join them into quads";
            QuadCleanupResult result;
            try
            {
                result = QuadCleanup.Clean(points, elements, fixedNodes, lastPasses);
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
            if (!triangleFree && result.NumberOfTriangles >= numOfGmshTriangles)
            {
                reason = oddReason != null && numOfGmshTriangles == 1 ? oddReason : notJoinedReason;
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
                    innerUv.Add(chart != null ? chart.ToUv(result.Points[i]) :
                                plane == null ? ThreeBlockMapping.FromFrame(w, result.Points[i]) : null);
                }
                else newIds[i] = -1;
            }
            if (plane != null && !SetPlaneParameters(surfaceId, plane, result, numOfBoundaryNodes, innerUv))
            {
                reason = "its inner nodes could not be placed on its surface";
                return false;
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
            if (cut || embedded)
            {
                reason = GetSeamJoinReason(newElements, nodeTags, numOfBoundaryNodes,
                                           cut ? "a seam" : "an edge inside it");
                if (reason != null) return false;
                if (cut && triangleFree &&
                    CountIrregularInnerNodes(GetNodeKeys(newElements, nodeTags, numOfBoundaryNodes), rimTags) >
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
            double minScaledJacobian = GetSpaceMinScaledJacobian(surfaceId, newElements, newXyz, newUv, orientation,
                                                                 poleNodes);
            double gmshMinScaledJacobian = GetSpaceMinScaledJacobian(surfaceId, elements, xyz, uv, orientation,
                                                                     poleNodes);
            double floor = triangleFree ? gmshMinScaledJacobian :
                           Math.Min(GetCornerFloor(surfaceId, segments, xyz, uv, orientation, poleNodes),
                                    gmshMinScaledJacobian);
            if (triangleFree && minScaledJacobian < floor) return false;
            if (minScaledJacobian < floor)
            {
                reason = "the cleaned mesh would have a corner of the scaled Jacobian " + Math.Round(minScaledJacobian, 3) +
                         ", below " + Math.Round(floor, 3);
                return false;
            }
            // The last passes add nodes, which may halve elements that are narrow already: their shape is held to
            // gmsh's
            if (lastPasses)
            {
                double minShape = GetSpaceMinShape(surfaceId, newElements, newXyz, newUv, orientation, poleNodes);
                double shapeFloor = Math.Min(ShapeFloor,
                                             GetSpaceMinShape(surfaceId, elements, xyz, uv, orientation, poleNodes));
                if (minShape < shapeFloor)
                {
                    reason = "the cleaned mesh would have an element of the shape " + Math.Round(minShape, 3) +
                             ", below " + Math.Round(shapeFloor, 3);
                    return false;
                }
            }
            IntPtr[] boundaryNodeTags = new IntPtr[numOfBoundaryNodes];
            for (int i = 0; i < numOfBoundaryNodes; i++) boundaryNodeTags[i] = nodeTags[i];
            GmshThreeBlockMesher.ReplaceSurfaceElements(surfaceId, boundaryNodeTags, newElements, innerUv.ToArray(),
                                                        innerXyz, false, true);
            numOfTriangles = result.NumberOfTriangles;
            reason = numOfTriangles == 0 ? null :
                     oddReason != null && numOfTriangles == 1 ? oddReason : notJoinedReason;
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
        /// The edges of the mesh of the face, by the ids of their nodes, that lie on a curve inside it: the segments of
        /// a curve that does not bound the face and whose every segment is an edge of the mesh.
        /// </summary>
        private static HashSet<long> GetEmbeddedEdges(List<int> boundaryEdgeIds, int[][] elements,
                                                      Dictionary<IntPtr, int> tagIndex)
        {
            HashSet<long> meshEdges = new HashSet<long>();
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                    meshEdges.Add(GetEdgeKey(element[c], element[(c + 1) % element.Length]));
            }
            HashSet<long> embeddedEdges = new HashSet<long>();
            Tuple<int, int>[] curveDimTags;
            Gmsh.Model.GetEntities(out curveDimTags, 1);
            foreach (var curveDimTag in curveDimTags)
            {
                if (boundaryEdgeIds.Contains(curveDimTag.Item2)) continue;
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] elementNodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 1,
                                            curveDimTag.Item2);
                List<long> segments = new List<long>();
                bool inside = elementTypes.Length > 0;
                for (int t = 0; t < elementTypes.Length && inside; t++)
                {
                    if (elementTypes[t] != (int)GmshElementType.LinearBeam)
                    {
                        inside = false;
                        break;
                    }
                    for (int e = 0; e < elementTags[t].Length && inside; e++)
                    {
                        int a = -1;
                        int b = -1;
                        inside = tagIndex.TryGetValue(elementNodeTags[t][2 * e], out a) &&
                                 tagIndex.TryGetValue(elementNodeTags[t][2 * e + 1], out b) &&
                                 meshEdges.Contains(GetEdgeKey(a, b));
                        if (inside) segments.Add(GetEdgeKey(a, b));
                    }
                }
                if (inside) embeddedEdges.UnionWith(segments);
            }
            return embeddedEdges;
        }
        /// <summary>
        /// The chart of the face round the pole at its degenerate edge, over the parameters of its other fixed nodes;
        /// null when the pole is not one of its surface.
        /// </summary>
        private static GmshPoleChart GetPoleChart(int surfaceId, int degenerateEdgeId, List<IntPtr> nodeTags,
                                                  double[][] xyz, double[][] uv, int numOfFixedNodes, double faceSize)
        {
            IntPtr[] tags;
            Gmsh.Model.Mesh.GetNodes(out tags, out _, 1, degenerateEdgeId, true, false);
            HashSet<IntPtr> poleTags = new HashSet<IntPtr>(tags);
            int pole = nodeTags.FindIndex(poleTags.Contains);
            if (pole < 0) return null;
            int p;
            double poleV;
            double farV;
            if (!GmshPoleChart.FindPole(surfaceId, xyz[pole], faceSize, out p, out poleV, out farV)) return null;
            List<double[]> faceUv = new List<double[]>();
            for (int i = 0; i < numOfFixedNodes; i++)
            {
                if (!poleTags.Contains(nodeTags[i])) faceUv.Add(uv[i]);
            }
            return new GmshPoleChart(surfaceId, xyz[pole], p, poleV, farV, faceUv);
        }
        /// <summary>
        /// The edges of the mesh, by the ids of their nodes, on the given curves.
        /// </summary>
        private static HashSet<long> GetCurveEdges(List<int> curveIds, Dictionary<IntPtr, int> tagIndex)
        {
            HashSet<long> edges = new HashSet<long>();
            foreach (var curveId in curveIds)
            {
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] elementNodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 1, curveId);
                for (int t = 0; t < elementTypes.Length; t++)
                {
                    if (elementTypes[t] != (int)GmshElementType.LinearBeam) continue;
                    for (int e = 0; e < elementTags[t].Length; e++)
                    {
                        int a;
                        int b;
                        if (tagIndex.TryGetValue(elementNodeTags[t][2 * e], out a) &&
                            tagIndex.TryGetValue(elementNodeTags[t][2 * e + 1], out b)) edges.Add(GetEdgeKey(a, b));
                    }
                }
            }
            return edges;
        }
        /// <summary>
        /// Cuts the mesh of a face open along the edges on curves inside it, so that the cleanup keeps them as the
        /// boundary they are: a node on such an edge appears once for every fan of elements around it that the edges
        /// separate, each copy a fixed node, so that a node at the free end of a curve or on a point inside the face
        /// stays one node. The copies follow the fixed nodes, before the inner ones, and are written back as the one
        /// node gmsh placed.
        /// </summary>
        private static void CutAlongEmbeddedEdges(HashSet<long> embeddedEdges, ref int numOfFixedNodes,
                                                  List<IntPtr> nodeTags, ref double[][] xyz, ref double[][] uv,
                                                  int[][] elements)
        {
            Dictionary<long, List<int>> edgeElements = new Dictionary<long, List<int>>();
            SortedDictionary<int, List<int>> nodeElements = new SortedDictionary<int, List<int>>();
            HashSet<int> cutNodes = new HashSet<int>();
            foreach (var key in embeddedEdges)
            {
                cutNodes.Add((int)(key >> 32));
                cutNodes.Add((int)(key & 0xFFFFFFFF));
            }
            for (int e = 0; e < elements.Length; e++)
            {
                int[] element = elements[e];
                for (int c = 0; c < element.Length; c++)
                {
                    long key = GetEdgeKey(element[c], element[(c + 1) % element.Length]);
                    List<int> ids;
                    if (!edgeElements.TryGetValue(key, out ids)) edgeElements.Add(key, ids = new List<int>());
                    ids.Add(e);
                    if (!cutNodes.Contains(element[c])) continue;
                    if (!nodeElements.TryGetValue(element[c], out ids))
                        nodeElements.Add(element[c], ids = new List<int>());
                    ids.Add(e);
                }
            }
            // The fans around every node on an edge inside, the elements joined across the edges that are not cut
            List<int> copyOf = new List<int>();
            Dictionary<long, int> cornerCopies = new Dictionary<long, int>();
            foreach (var entry in nodeElements)
            {
                int node = entry.Key;
                List<int> fan = entry.Value;
                int[] parent = new int[fan.Count];
                for (int i = 0; i < parent.Length; i++) parent[i] = i;
                for (int i = 0; i < fan.Count; i++)
                {
                    int[] element = elements[fan[i]];
                    int c = Array.IndexOf(element, node);
                    foreach (var other in new int[] { element[(c + 1) % element.Length],
                                                      element[(c + element.Length - 1) % element.Length] })
                    {
                        long key = GetEdgeKey(node, other);
                        if (embeddedEdges.Contains(key)) continue;
                        foreach (var e in edgeElements[key])
                        {
                            int j = fan.IndexOf(e);
                            if (j < 0 || j == i) continue;
                            int rootI = GetRoot(parent, i);
                            int rootJ = GetRoot(parent, j);
                            if (rootI != rootJ) parent[Math.Max(rootI, rootJ)] = Math.Min(rootI, rootJ);
                        }
                    }
                }
                Dictionary<int, int> rootCopies = new Dictionary<int, int>();
                for (int i = 0; i < fan.Count; i++)
                {
                    int root = GetRoot(parent, i);
                    int copy;
                    if (!rootCopies.TryGetValue(root, out copy))
                    {
                        copy = rootCopies.Count == 0 ? -1 : copyOf.Count;
                        if (copy >= 0) copyOf.Add(node);
                        rootCopies.Add(root, copy);
                    }
                    if (copy >= 0) cornerCopies.Add(((long)fan[i] << 32) | (uint)node, copy);
                }
            }
            if (copyOf.Count == 0) return;
            // The copies after the fixed nodes, the inner nodes after them
            int numOfCopies = copyOf.Count;
            int numOfNodes = nodeTags.Count;
            List<IntPtr> cutNodeTags = nodeTags.GetRange(0, numOfFixedNodes);
            List<double[]> cutXyz = xyz.Take(numOfFixedNodes).ToList();
            List<double[]> cutUv = uv.Take(numOfFixedNodes).ToList();
            foreach (var node in copyOf)
            {
                cutNodeTags.Add(nodeTags[node]);
                cutXyz.Add(xyz[node]);
                cutUv.Add(new double[] { uv[node][0], uv[node][1] });
            }
            cutNodeTags.AddRange(nodeTags.GetRange(numOfFixedNodes, numOfNodes - numOfFixedNodes));
            cutXyz.AddRange(xyz.Skip(numOfFixedNodes));
            cutUv.AddRange(uv.Skip(numOfFixedNodes));
            for (int e = 0; e < elements.Length; e++)
            {
                for (int c = 0; c < elements[e].Length; c++)
                {
                    int node = elements[e][c];
                    int copy;
                    if (cornerCopies.TryGetValue(((long)e << 32) | (uint)node, out copy))
                        elements[e][c] = numOfFixedNodes + copy;
                    else if (node >= numOfFixedNodes) elements[e][c] = node + numOfCopies;
                }
            }
            nodeTags.Clear();
            nodeTags.AddRange(cutNodeTags);
            xyz = cutXyz.ToArray();
            uv = cutUv.ToArray();
            numOfFixedNodes += numOfCopies;
        }
        private static int GetRoot(int[] parent, int i)
        {
            while (parent[i] != i) i = parent[i] = parent[parent[i]];
            return i;
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
        /// Why the elements of a face cut along its seams or the curves inside it cannot be written with each node on a
        /// cut once: an element that would use a node twice, or two elements of the same nodes; null when they can.
        /// </summary>
        private static string GetSeamJoinReason(int[][] elements, List<IntPtr> nodeTags, int numOfBoundaryNodes,
                                                string cutName)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var element in GetNodeKeys(elements, nodeTags, numOfBoundaryNodes))
            {
                long[] nodes = (long[])element.Clone();
                Array.Sort(nodes);
                for (int c = 1; c < nodes.Length; c++)
                {
                    if (nodes[c] == nodes[c - 1])
                        return "the cleanup would join the two sides of " + cutName + " in one element";
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
        /// The floor of the scaled Jacobian a face with triangles is held to: SubmapFaceDetector.MinScaledJacobian, or
        /// what the sharpest corner of its boundary allows a quad in it when that is lower, SharpCornerShare times the
        /// sine of a corner below 90 degrees in space, measured between the boundary segments at the node against the
        /// normal of the surface.
        /// </summary>
        private static double GetCornerFloor(int surfaceId, List<int[]> segments, double[][] xyz, double[][] uv,
                                             double orientation, HashSet<int> poleNodes)
        {
            double floor = SubmapFaceDetector.MinScaledJacobian;
            Dictionary<int, int> next = new Dictionary<int, int>();
            Dictionary<int, int> previous = new Dictionary<int, int>();
            HashSet<int> branchNodes = new HashSet<int>();
            foreach (var segment in segments)
            {
                if (next.ContainsKey(segment[0])) branchNodes.Add(segment[0]);
                else next.Add(segment[0], segment[1]);
                if (previous.ContainsKey(segment[1])) branchNodes.Add(segment[1]);
                else previous.Add(segment[1], segment[0]);
            }
            List<int> nodes = next.Keys.Where(node => previous.ContainsKey(node) && !branchNodes.Contains(node) &&
                                                       !poleNodes.Contains(node))
                                       .OrderBy(node => node).ToList();
            if (nodes.Count == 0) return floor;
            double[][] normals = GetNormals(surfaceId, nodes.Select(node => uv[node]).ToArray());
            for (int i = 0; i < nodes.Count; i++)
            {
                double[] p = xyz[nodes[i]];
                double[] a = new double[3];
                double[] b = new double[3];
                for (int d = 0; d < 3; d++)
                {
                    a[d] = xyz[next[nodes[i]]][d] - p[d];
                    b[d] = xyz[previous[nodes[i]]][d] - p[d];
                }
                double[] normal = normals[i];
                double cross = (a[1] * b[2] - a[2] * b[1]) * normal[0] + (a[2] * b[0] - a[0] * b[2]) * normal[1] +
                               (a[0] * b[1] - a[1] * b[0]) * normal[2];
                double angle = Math.Atan2(orientation * cross, a[0] * b[0] + a[1] * b[1] + a[2] * b[2]);
                if (angle > 0 && angle < 0.5 * Math.PI) floor = Math.Min(floor, SharpCornerShare * Math.Sin(angle));
            }
            return floor;
        }
        /// <summary>
        /// The elements with a corner that folds or has no area, against the way most of the area turns.
        /// </summary>
        private static int CountFoldedElements(double[][] points, int[][] elements)
        {
            double area = 0;
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                {
                    double[] p = points[element[c]];
                    double[] q = points[element[(c + 1) % element.Length]];
                    area += p[0] * q[1] - q[0] * p[1];
                }
            }
            return QuadSmoothing.CountFoldedElements(points, elements, area < 0 ? -1 : 1);
        }
        /// <summary>
        /// The nodes projected on the plane through their centre normal to the mean normal of the elements, and the
        /// plane as its origin and two axes; null when the elements have no mean normal.
        /// </summary>
        private static double[][] GetProjectedPoints(double[][] xyz, int[][] elements, out double[][] plane)
        {
            plane = null;
            double[] normal = new double[3];
            foreach (var element in elements)
            {
                double[] a = xyz[element[0]];
                double[] b = xyz[element[1]];
                double[] c = xyz[element[2]];
                double[] d = element.Length == 4 ? xyz[element[3]] : a;
                double[] first = new double[] { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                double[] second = new double[] { d[0] - b[0], d[1] - b[1], d[2] - b[2] };
                if (element.Length == 3) second = new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
                normal[0] += first[1] * second[2] - first[2] * second[1];
                normal[1] += first[2] * second[0] - first[0] * second[2];
                normal[2] += first[0] * second[1] - first[1] * second[0];
            }
            double length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            if (!(length > 0)) return null;
            for (int k = 0; k < 3; k++) normal[k] /= length;
            double[] axisU = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double along = axisU[0] * normal[0] + axisU[1] * normal[1] + axisU[2] * normal[2];
            for (int k = 0; k < 3; k++) axisU[k] -= along * normal[k];
            length = Math.Sqrt(axisU[0] * axisU[0] + axisU[1] * axisU[1] + axisU[2] * axisU[2]);
            for (int k = 0; k < 3; k++) axisU[k] /= length;
            double[] axisV = new double[] { normal[1] * axisU[2] - normal[2] * axisU[1],
                                            normal[2] * axisU[0] - normal[0] * axisU[2],
                                            normal[0] * axisU[1] - normal[1] * axisU[0] };
            double[] origin = new double[3];
            foreach (var p in xyz)
            {
                for (int k = 0; k < 3; k++) origin[k] += p[k] / xyz.Length;
            }
            double[][] points = new double[xyz.Length][];
            for (int i = 0; i < xyz.Length; i++)
            {
                double[] r = new double[] { xyz[i][0] - origin[0], xyz[i][1] - origin[1], xyz[i][2] - origin[2] };
                points[i] = new double[] { r[0] * axisU[0] + r[1] * axisU[1] + r[2] * axisU[2],
                                           r[0] * axisV[0] + r[1] * axisV[1] + r[2] * axisV[2] };
            }
            plane = new double[][] { origin, axisU, axisV };
            return points;
        }
        /// <summary>
        /// The parameters of the inner nodes the cleanup placed in the projection plane, at the points of the surface
        /// nearest to them; false when they could not be found.
        /// </summary>
        private static bool SetPlaneParameters(int surfaceId, double[][] plane, QuadCleanupResult result,
                                               int numOfBoundaryNodes, List<double[]> innerUv)
        {
            List<double> flatXyz = new List<double>();
            for (int i = numOfBoundaryNodes; i < result.Points.Length; i++)
            {
                if (!result.IsNodeUsed[i]) continue;
                for (int k = 0; k < 3; k++)
                    flatXyz.Add(plane[0][k] + result.Points[i][0] * plane[1][k] + result.Points[i][1] * plane[2][k]);
            }
            if (flatXyz.Count == 0) return true;
            double[] flatUv;
            Gmsh.Model.GetParametrization(2, surfaceId, flatXyz.ToArray(), out flatUv);
            if (flatUv == null || flatUv.Length != 2 * innerUv.Count) return false;
            for (int i = 0; i < innerUv.Count; i++) innerUv[i] = new double[] { flatUv[2 * i], flatUv[2 * i + 1] };
            return true;
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
                                                        double orientation, HashSet<int> poleNodes = null)
        {
            double[][] normals = GetNormals(surfaceId, uv);
            SetPoleNormals(normals, elements, xyz, poleNodes, orientation);
            double min = 1;
            foreach (var element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                    min = Math.Min(min, GetSpaceScaledJacobian(element, c, xyz, normals, orientation));
            }
            return min;
        }
        /// <summary>
        /// The worst shape of the elements in space: the scaled Jacobian of a corner times 2 |a| |b| / (|a|^2 + |b|^2),
        /// a and b the edges at the corner, as QuadCleanup.GetMinShape measures it in the plane.
        /// </summary>
        private static double GetSpaceMinShape(int surfaceId, int[][] elements, double[][] xyz, double[][] uv,
                                               double orientation, HashSet<int> poleNodes)
        {
            double[][] normals = GetNormals(surfaceId, uv);
            SetPoleNormals(normals, elements, xyz, poleNodes, orientation);
            double min = 1;
            foreach (var element in elements)
            {
                int n = element.Length;
                for (int c = 0; c < n; c++)
                {
                    double aLength2 = 0;
                    double bLength2 = 0;
                    for (int d = 0; d < 3; d++)
                    {
                        double a = xyz[element[(c + 1) % n]][d] - xyz[element[c]][d];
                        double b = xyz[element[(c + n - 1) % n]][d] - xyz[element[c]][d];
                        aLength2 += a * a;
                        bLength2 += b * b;
                    }
                    double factor = aLength2 + bLength2 > 0 ?
                                    2 * Math.Sqrt(aLength2 * bLength2) / (aLength2 + bLength2) : 0;
                    min = Math.Min(min, GetSpaceScaledJacobian(element, c, xyz, normals, orientation) * factor);
                }
            }
            return min;
        }
        /// <summary>
        /// The normal at a pole, where that of the surface is not defined (the apex of a cone), as the mean of the
        /// normals of the corners of the elements there, turned so that the orientation measures them as they turn.
        /// </summary>
        private static void SetPoleNormals(double[][] normals, int[][] elements, double[][] xyz, HashSet<int> poleNodes,
                                           double orientation)
        {
            if (poleNodes == null || poleNodes.Count == 0) return;
            Dictionary<int, double[]> sums = new Dictionary<int, double[]>();
            foreach (var element in elements)
            {
                int n = element.Length;
                for (int c = 0; c < n; c++)
                {
                    if (!poleNodes.Contains(element[c])) continue;
                    double[] p = xyz[element[c]];
                    double[] a = xyz[element[(c + 1) % n]];
                    double[] b = xyz[element[(c + n - 1) % n]];
                    double[] cross = new double[] { (a[1] - p[1]) * (b[2] - p[2]) - (a[2] - p[2]) * (b[1] - p[1]),
                                                    (a[2] - p[2]) * (b[0] - p[0]) - (a[0] - p[0]) * (b[2] - p[2]),
                                                    (a[0] - p[0]) * (b[1] - p[1]) - (a[1] - p[1]) * (b[0] - p[0]) };
                    double length = Math.Sqrt(cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]);
                    if (!(length > 0)) continue;
                    double[] sum;
                    if (!sums.TryGetValue(element[c], out sum)) sums.Add(element[c], sum = new double[3]);
                    for (int d = 0; d < 3; d++) sum[d] += cross[d] / length;
                }
            }
            foreach (var entry in sums)
            {
                double length = Math.Sqrt(entry.Value.Sum(v => v * v));
                if (length > 0) normals[entry.Key] = entry.Value.Select(v => orientation * v / length).ToArray();
            }
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
