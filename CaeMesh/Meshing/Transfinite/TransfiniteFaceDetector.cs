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
    /// Each corner and each joint of a side is judged on its own, so a face is also held to the grid its sides span:
    /// a side chained through many nearly straight joints can still wind far across the face, and a grid stretched
    /// between it and the other sides folds.
    /// </summary>
    public static class TransfiniteFaceDetector
    {
        // Variables
        private const double _cornerAngleToleranceDeg = 0.1;
        private const int _foldCheckDivisions = 16;


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
        /// <summary>
        /// The corner the polar grid of a three-sided face of the part collapses, by the lengths of its sides on the
        /// geometry, or -1 for a face with four sides.
        /// </summary>
        public static int GetPolarCornerIndex(FeMesh mesh, BasePart part, TransfiniteFace face)
        {
            if (face.NumberOfSides != 3) return -1;
            //
            VisualizationData vis = part.Visualization;
            Func<int, double> getEdgeLength = edgeId =>
            {
                if (vis.EdgeLengths != null && edgeId < vis.EdgeLengths.Length) return vis.EdgeLengths[edgeId];
                double length = 0;
                foreach (var cellId in vis.EdgeCellIdsByEdge[edgeId])
                {
                    int[] cell = vis.EdgeCells[cellId];
                    double[] a = mesh.Nodes[cell[0]].Coor;
                    double[] b = mesh.Nodes[cell[cell.Length - 1]].Coor;
                    length += Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) +
                                        (b[2] - a[2]) * (b[2] - a[2]));
                }
                return length;
            };
            return face.GetPolarCornerIndex(face.SideEdgeIds.Select(side => side.Sum(getEdgeLength)).ToArray());
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
            TransfiniteFace transfiniteFace = new TransfiniteFace(faceId, cornerNodeIds, cornerAnglesDeg, sideEdgeIds);
            if (DoesGridFold(mesh, vis, loopEdgeIds, loopVertexIds, corners, transfiniteFace)) return null;
            return transfiniteFace;
        }
        /// <summary>
        /// Whether the structured grid the sides of a face span folds. The sides are sampled at equal steps of their
        /// length, and the grid is their transfinite interpolation: between the four sides of a four-sided face, and
        /// for a three-sided face its polar grid, collapsed into the corner GetPolarCornerIndex picks, whose lines
        /// follow the sides however thin the face. A cell turned against a neighbour - their normals pointing to
        /// opposite sides - is a fold. A curved face turns its cells gradually, and neighbours never face apart; only
        /// a side that winds across the grid turns one over.
        /// </summary>
        private static bool DoesGridFold(FeMesh mesh, VisualizationData vis, int[] loopEdgeIds, int[] loopVertexIds,
                                         List<int> corners, TransfiniteFace transfiniteFace)
        {
            int m = _foldCheckDivisions;
            int n = loopEdgeIds.Length;
            int numOfSides = corners.Count;
            double[][][] sides = new double[numOfSides][][];
            double[] sideLengths = new double[numOfSides];
            for (int c = 0; c < numOfSides; c++)
            {
                List<double[]> polyline = new List<double[]>();
                int end = corners[(c + 1) % numOfSides];
                int k = corners[c];
                do
                {
                    List<double[]> edgePoints = GetEdgePoints(mesh, vis, loopEdgeIds[k], loopVertexIds[k]);
                    if (edgePoints == null) return false;
                    if (polyline.Count > 0) edgePoints.RemoveAt(0);
                    polyline.AddRange(edgePoints);
                    k = (k + 1) % n;
                }
                while (k != end);
                sides[c] = Resample(polyline, m, out sideLengths[c]);
                if (sides[c] == null) return false;
            }
            //
            if (numOfSides == 4)
                return DoesTransfiniteGridFold(sides[0], sides[1], sides[2].Reverse().ToArray(),
                                               sides[3].Reverse().ToArray());
            //
            int polar = transfiniteFace.GetPolarCornerIndex(sideLengths);
            double[][] collapsed = Enumerable.Repeat(sides[polar][0], m + 1).ToArray();
            double[][] opposite = sides[(polar + 2) % 3].Reverse().ToArray();
            return DoesTransfiniteGridFold(sides[polar], sides[(polar + 1) % 3], opposite, collapsed);
        }
        /// <summary>
        /// Whether the transfinite (Coons) grid between four sides sampled at the same number of points folds. The
        /// bottom and the top run in the first direction of the grid, the left and the right in the second, every one
        /// from the corner shared with the left or the bottom side.
        /// </summary>
        private static bool DoesTransfiniteGridFold(double[][] bottom, double[][] right, double[][] top, double[][] left)
        {
            int m = bottom.Length - 1;
            double[][,] grid = new double[3][,];
            for (int d = 0; d < 3; d++) grid[d] = new double[m + 1, m + 1];
            for (int i = 0; i <= m; i++)
            {
                double u = (double)i / m;
                for (int j = 0; j <= m; j++)
                {
                    double v = (double)j / m;
                    for (int d = 0; d < 3; d++)
                    {
                        grid[d][i, j] = (1 - v) * bottom[i][d] + v * top[i][d] + (1 - u) * left[j][d] + u * right[j][d] -
                                        ((1 - u) * (1 - v) * bottom[0][d] + u * (1 - v) * bottom[m][d] +
                                         (1 - u) * v * top[0][d] + u * v * top[m][d]);
                    }
                }
            }
            double[][,] normals = new double[3][,];
            for (int d = 0; d < 3; d++) normals[d] = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    double[] diagonal1 = new double[3];
                    double[] diagonal2 = new double[3];
                    for (int d = 0; d < 3; d++)
                    {
                        diagonal1[d] = grid[d][i + 1, j + 1] - grid[d][i, j];
                        diagonal2[d] = grid[d][i, j + 1] - grid[d][i + 1, j];
                    }
                    normals[0][i, j] = diagonal1[1] * diagonal2[2] - diagonal1[2] * diagonal2[1];
                    normals[1][i, j] = diagonal1[2] * diagonal2[0] - diagonal1[0] * diagonal2[2];
                    normals[2][i, j] = diagonal1[0] * diagonal2[1] - diagonal1[1] * diagonal2[0];
                }
            }
            Func<int, int, int, int, double> dot = (i1, j1, i2, j2) =>
                normals[0][i1, j1] * normals[0][i2, j2] + normals[1][i1, j1] * normals[1][i2, j2] +
                normals[2][i1, j1] * normals[2][i2, j2];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (i + 1 < m && dot(i, j, i + 1, j) < 0) return true;
                    if (j + 1 < m && dot(i, j, i, j + 1) < 0) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// The points of an edge in order from the given vertex, read off its edge cells. Null when the cells do
        /// not chain from the vertex.
        /// </summary>
        private static List<double[]> GetEdgePoints(FeMesh mesh, VisualizationData vis, int edgeId, int startNodeId)
        {
            int[] cellIds = vis.EdgeCellIdsByEdge[edgeId];
            bool[] used = new bool[cellIds.Length];
            List<double[]> points = new List<double[]> { mesh.Nodes[startNodeId].Coor };
            int current = startNodeId;
            for (int step = 0; step < cellIds.Length; step++)
            {
                int next = -1;
                for (int c = 0; c < cellIds.Length && next < 0; c++)
                {
                    if (used[c]) continue;
                    int[] cell = vis.EdgeCells[cellIds[c]];
                    if (cell[0] == current) next = cell[cell.Length - 1];
                    else if (cell[cell.Length - 1] == current) next = cell[0];
                    if (next >= 0) used[c] = true;
                }
                if (next < 0) return null;
                points.Add(mesh.Nodes[next].Coor);
                current = next;
            }
            return points;
        }
        /// <summary>
        /// The points at equal steps of the length of a polyline, its two ends included, and its length.
        /// </summary>
        private static double[][] Resample(List<double[]> polyline, int divisions, out double length)
        {
            double[] lengths = new double[polyline.Count];
            for (int i = 1; i < polyline.Count; i++)
            {
                double[] a = polyline[i - 1];
                double[] b = polyline[i];
                lengths[i] = lengths[i - 1] + Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) +
                                                        (b[2] - a[2]) * (b[2] - a[2]));
            }
            double total = lengths[polyline.Count - 1];
            length = total;
            if (total <= 0) return null;
            //
            double[][] samples = new double[divisions + 1][];
            int segment = 1;
            for (int s = 0; s <= divisions; s++)
            {
                double target = total * s / divisions;
                while (segment < polyline.Count - 1 && lengths[segment] < target) segment++;
                double span = lengths[segment] - lengths[segment - 1];
                double f = span > 0 ? Math.Max(0, Math.Min(1, (target - lengths[segment - 1]) / span)) : 0;
                double[] a = polyline[segment - 1];
                double[] b = polyline[segment];
                samples[s] = new double[] { a[0] + f * (b[0] - a[0]), a[1] + f * (b[1] - a[1]),
                                            a[2] + f * (b[2] - a[2]) };
            }
            return samples;
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
