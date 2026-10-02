// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// The one definition of the faces every mesher lays a structured grid over, and the mesh type colors show.
    /// A face is structured when its edges form one boundary loop and the loop has three or four corners: the
    /// vertices where its edges meet at the transfinite angle or sharper. Edges that meet at a flatter angle are
    /// one side. Every corner of a four-sided face lies within 180° minus the angle and the angle; a three-sided
    /// face may have its sharpest corner below that range, since its singular point moves towards that corner.
    /// </summary>
    public static class TransfiniteFaceDetector
    {
        // Variables
        private const double _cornerAngleToleranceDeg = 0.1;


        // Methods
        public static TransfiniteFace[] Detect(FeMesh mesh, BasePart part, bool threeSided, bool fourSided,
                                               double transfiniteAngleDeg)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || (!threeSided && !fourSided))
                return new TransfiniteFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<TransfiniteFace> transfiniteFaces = new List<TransfiniteFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                TransfiniteFace transfiniteFace = DetectFace(mesh, vis, i, threeSided, fourSided, transfiniteAngleDeg,
                                                             edgeIdVertexIds);
                if (transfiniteFace != null) transfiniteFaces.Add(transfiniteFace);
            }
            return transfiniteFaces.ToArray();
        }
        public static bool[] GetTransfiniteFaceFlags(FeMesh mesh, BasePart part, bool threeSided, bool fourSided,
                                                     double transfiniteAngleDeg)
        {
            bool[] transfinite = new bool[part.Visualization.FaceCount];
            foreach (var transfiniteFace in Detect(mesh, part, threeSided, fourSided, transfiniteAngleDeg))
                transfinite[transfiniteFace.FaceId] = true;
            return transfinite;
        }
        private static TransfiniteFace DetectFace(FeMesh mesh, VisualizationData vis, int faceId, bool threeSided,
                                                  bool fourSided, double transfiniteAngleDeg,
                                                  Dictionary<int, int[]> edgeIdVertexIds)
        {
            int[] faceEdgeIds = vis.FaceEdgeIds[faceId];
            if (faceEdgeIds == null || faceEdgeIds.Length < 3) return null;
            //
            int[] loopEdgeIds;
            int[] loopVertexIds;
            if (!GetBoundaryLoop(vis, faceEdgeIds, edgeIdVertexIds, out loopEdgeIds, out loopVertexIds)) return null;
            // loopVertexIds[k] is the vertex where loop edge k - 1 ends and loop edge k starts
            int n = loopEdgeIds.Length;
            double[] anglesDeg = new double[n];
            for (int k = 0; k < n; k++)
            {
                double[] incoming = GetEdgeTangent(mesh, vis, loopEdgeIds[(k + n - 1) % n], loopVertexIds[k]);
                double[] outgoing = GetEdgeTangent(mesh, vis, loopEdgeIds[k], loopVertexIds[k]);
                anglesDeg[k] = GetAngleDeg(incoming, outgoing);
                if (double.IsNaN(anglesDeg[k])) return null;
            }
            //
            double maxCornerAngleDeg = transfiniteAngleDeg + _cornerAngleToleranceDeg;
            double minCornerAngleDeg = 180 - transfiniteAngleDeg - _cornerAngleToleranceDeg;
            List<int> corners = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (anglesDeg[k] <= maxCornerAngleDeg) corners.Add(k);
            }
            if (!((corners.Count == 4 && fourSided) || (corners.Count == 3 && threeSided))) return null;
            //
            int sharpest = corners.OrderBy(k => anglesDeg[k]).First();
            foreach (var k in corners)
            {
                if (anglesDeg[k] >= minCornerAngleDeg) continue;
                if (corners.Count == 3 && k == sharpest) continue;
                return null;
            }
            //
            int[] cornerNodeIds = new int[corners.Count];
            double[] cornerAnglesDeg = new double[corners.Count];
            int[][] sideEdgeIds = new int[corners.Count][];
            for (int c = 0; c < corners.Count; c++)
            {
                cornerNodeIds[c] = loopVertexIds[corners[c]];
                cornerAnglesDeg[c] = anglesDeg[corners[c]];
                //
                List<int> side = new List<int>();
                int end = corners[(c + 1) % corners.Count];
                int k = corners[c];
                do
                {
                    side.Add(loopEdgeIds[k]);
                    k = (k + 1) % n;
                }
                while (k != end);
                sideEdgeIds[c] = side.ToArray();
            }
            return new TransfiniteFace(faceId, cornerNodeIds, cornerAnglesDeg, sideEdgeIds);
        }
        /// <summary>
        /// Orders the edges of a face head to tail into one loop. A face with a hole, an edge inside it, a closed
        /// edge or a seam has edges that do not make one simple loop, and has none.
        /// </summary>
        private static bool GetBoundaryLoop(VisualizationData vis, int[] faceEdgeIds,
                                            Dictionary<int, int[]> edgeIdVertexIds,
                                            out int[] loopEdgeIds, out int[] loopVertexIds)
        {
            loopEdgeIds = null;
            loopVertexIds = null;
            //
            int[] vertexIds;
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in faceEdgeIds)
            {
                if (!edgeIdVertexIds.TryGetValue(edgeId, out vertexIds))
                {
                    vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                    edgeIdVertexIds.Add(edgeId, vertexIds);
                }
                if (vertexIds.Length != 2 || vertexIds[0] == vertexIds[1]) return false;
                //
                foreach (var vertexId in vertexIds)
                {
                    List<int> edgeIds;
                    if (!vertexIdEdgeIds.TryGetValue(vertexId, out edgeIds))
                    {
                        edgeIds = new List<int>();
                        vertexIdEdgeIds.Add(vertexId, edgeIds);
                    }
                    edgeIds.Add(edgeId);
                }
            }
            if (vertexIdEdgeIds.Values.Any(edgeIds => edgeIds.Count != 2)) return false;
            //
            List<int> edgeLoop = new List<int>();
            List<int> vertexLoop = new List<int>();
            int currentEdgeId = faceEdgeIds[0];
            int currentVertexId = edgeIdVertexIds[currentEdgeId][0];
            do
            {
                vertexLoop.Add(currentVertexId);
                edgeLoop.Add(currentEdgeId);
                int[] currentVertexIds = edgeIdVertexIds[currentEdgeId];
                currentVertexId = currentVertexIds[0] == currentVertexId ? currentVertexIds[1] : currentVertexIds[0];
                List<int> nextEdgeIds = vertexIdEdgeIds[currentVertexId];
                currentEdgeId = nextEdgeIds[0] == currentEdgeId ? nextEdgeIds[1] : nextEdgeIds[0];
            }
            while (currentEdgeId != faceEdgeIds[0] && edgeLoop.Count <= faceEdgeIds.Length);
            //
            if (edgeLoop.Count != faceEdgeIds.Length) return false;
            //
            loopEdgeIds = edgeLoop.ToArray();
            loopVertexIds = vertexLoop.ToArray();
            return true;
        }
        /// <summary>
        /// The direction an edge leaves one of its vertices in, read off the edge cell at that vertex.
        /// </summary>
        private static double[] GetEdgeTangent(FeMesh mesh, VisualizationData vis, int edgeId, int vertexNodeId)
        {
            double[] vertex = mesh.Nodes[vertexNodeId].Coor;
            foreach (var cellId in vis.EdgeCellIdsByEdge[edgeId])
            {
                int[] cell = vis.EdgeCells[cellId];
                double[] other = null;
                if (cell[0] == vertexNodeId) other = mesh.Nodes[cell[cell.Length - 1]].Coor;
                else if (cell[cell.Length - 1] == vertexNodeId) other = mesh.Nodes[cell[0]].Coor;
                if (other != null) return new double[] { other[0] - vertex[0], other[1] - vertex[1], other[2] - vertex[2] };
            }
            return new double[3];
        }
        private static double GetAngleDeg(double[] a, double[] b)
        {
            double lengthA = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]);
            double lengthB = Math.Sqrt(b[0] * b[0] + b[1] * b[1] + b[2] * b[2]);
            if (lengthA == 0 || lengthB == 0) return double.NaN;
            //
            double cos = (a[0] * b[0] + a[1] * b[1] + a[2] * b[2]) / (lengthA * lengthB);
            return Math.Acos(Math.Max(-1, Math.Min(1, cos))) * 180 / Math.PI;
        }
    }
}
