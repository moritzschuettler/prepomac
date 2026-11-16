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
    /// corner gmsh left, whichever is lower, measured in space against the normal of the surface. A face with a seam or
    /// a degenerate edge, whose parameters do not run evenly over it, or with a node on an edge or a point inside it is
    /// left as gmsh meshed it.
    /// </summary>
    public static class GmshQuadMesher
    {
        // Variables                                                                                                                
        private static readonly double TriangleScale = 2 / Math.Sqrt(3);


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
            if (numOfTriangles == 0) return false;
            // The nodes on the edges of the face, which stay
            int[] edgeIds;
            Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
            HashSet<int> uniqueEdgeIds = new HashSet<int>();
            HashSet<IntPtr> boundaryTags = new HashSet<IntPtr>();
            IntPtr[] tags;
            double[] coor;
            foreach (var signedEdgeId in edgeIds)
            {
                int edgeId = Math.Abs(signedEdgeId);
                if (!uniqueEdgeIds.Add(edgeId))
                {
                    reason = "a seam of its surface runs along Edge " + edgeId;
                    return false;
                }
                double length;
                Gmsh.Model.OCC.GetMass(1, edgeId, out length);
                if (!(length > 0))
                {
                    reason = "Edge " + edgeId + " of it is degenerate";
                    return false;
                }
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, true, false);
                foreach (var tag in tags) boundaryTags.Add(tag);
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
            List<int[]> segments = GetBoundarySegments(elements);
            double[][][] segmentUv = new double[segments.Count][][];
            double[][][] segmentXyz = new double[segments.Count][][];
            for (int s = 0; s < segments.Count; s++)
            {
                segmentUv[s] = new double[][] { uv[segments[s][0]], uv[segments[s][1]] };
                segmentXyz[s] = new double[][] { xyz[segments[s][0]], xyz[segments[s][1]] };
            }
            double[] w = ThreeBlockMapping.GetMetricFrame(segmentUv, segmentXyz);
            double[][] points = new double[numOfNodes][];
            for (int i = 0; i < numOfNodes; i++) points[i] = ThreeBlockMapping.ToFrame(w, uv[i]);
            if (!AreAreasOfOneSign(points, elements))
            {
                reason = "its parameters do not run evenly over it";
                return false;
            }
            bool oddTotal = segments.Count % 2 == 1;
            string oddReason = "the elements around its boundary add up to an odd number, " + segments.Count;
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
            if (result.NumberOfTriangles >= numOfTriangles)
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
            int[] newIds = new int[numOfNodes];
            List<double[]> innerUv = new List<double[]>();
            for (int i = 0; i < numOfNodes; i++)
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
            double floor = Math.Min(SubmapFaceDetector.MinScaledJacobian,
                                    GetSpaceMinScaledJacobian(surfaceId, elements, xyz, uv, orientation));
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
        private static double[][] GetNormals(int surfaceId, double[][] uv)
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
        private static double GetSpaceScaledJacobian(int[] element, int c, double[][] xyz, double[][] normals,
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
