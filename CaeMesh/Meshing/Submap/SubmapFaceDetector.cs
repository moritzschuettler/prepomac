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

namespace CaeMesh
{
    /// <summary>
    /// Finds the faces of a part that SubmapMapping meshes: faces of one simple boundary loop whose vertices are all
    /// ends, sides or corners by the interior angle of the face there. With T the transfinite angle, a vertex is an end
    /// at an angle from 180° - T to T, a side above that and below 360° - T, and a corner from 360° - T to 180° + T;
    /// any other angle leaves the face free. The interior angle is the sum of the angles the cells of the face make at
    /// the vertex, which, unlike the angle between the two edges, tells a convex corner from a re-entrant one. A face
    /// is submapped when its ends outnumber its corners by four, the turn of a closed loop, and it has a corner: one
    /// without is the business of TransfiniteFaceDetector.
    /// A face that is a height field over the plane of its boundary loop is judged by the grid SubmapMapping lays over
    /// it in that plane, with counts in proportion to the lengths of its edges, and is left free when that grid folds
    /// or is poor; the meshers judge the grid of the counts they give it again.
    /// </summary>
    public static class SubmapFaceDetector
    {
        // Variables                                                                                                                
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of a submapped face; a face whose grid has a worse one is meshed
        /// free.
        /// </summary>
        public const double MinScaledJacobian = 0.2;
        private const int NumOfOutlineDivisions = 64;
        private const int NumOfOutlineRefinements = 3;
        private static readonly int[] DirectionI = new int[] { 1, 0, -1, 0 };
        private static readonly int[] DirectionJ = new int[] { 0, 1, 0, -1 };


        // Methods                                                                                                                  
        public static SubmapFace[] Detect(FeMesh mesh, BasePart part, double transfiniteAngleDeg)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || vis.CellIdsByFace == null)
                return new SubmapFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<SubmapFace> submapFaces = new List<SubmapFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                SubmapFace submapFace = DetectFace(mesh, vis, i, transfiniteAngleDeg, edgeIdVertexIds);
                if (submapFace != null) submapFaces.Add(submapFace);
            }
            return submapFaces.ToArray();
        }
        /// <summary>
        /// Whether a vertex with the given interior angle is an end (1), a side (0) or a corner (-1) of a submapped face;
        /// null when it is none of them.
        /// </summary>
        public static int? GetTurn(double angleDeg, double transfiniteAngleDeg)
        {
            double tolerance = TransfiniteFaceDetector._cornerAngleToleranceDeg;
            if (angleDeg >= 180 - transfiniteAngleDeg - tolerance && angleDeg <= transfiniteAngleDeg + tolerance) return 1;
            if (angleDeg > transfiniteAngleDeg + tolerance && angleDeg < 360 - transfiniteAngleDeg - tolerance) return 0;
            if (angleDeg >= 360 - transfiniteAngleDeg - tolerance && angleDeg <= 180 + transfiniteAngleDeg + tolerance)
                return -1;
            return null;
        }
        private static SubmapFace DetectFace(FeMesh mesh, VisualizationData vis, int faceId, double transfiniteAngleDeg,
                                             Dictionary<int, int[]> edgeIdVertexIds)
        {
            int[] faceEdgeIds = vis.FaceEdgeIds[faceId];
            if (faceEdgeIds == null || faceEdgeIds.Length < 6 || faceId >= vis.CellIdsByFace.Length) return null;
            //
            int[] loopEdgeIds;
            int[] loopVertexIds;
            if (!TransfiniteFaceDetector.GetBoundaryLoop(vis, faceEdgeIds, edgeIdVertexIds, out loopEdgeIds,
                                                         out loopVertexIds)) return null;
            //
            int n = loopEdgeIds.Length;
            double[] anglesDeg = GetInteriorAnglesDeg(mesh, vis, faceId, loopVertexIds);
            if (anglesDeg == null) return null;
            int[] turns = new int[n];
            int numOfEnds = 0;
            int numOfCorners = 0;
            for (int k = 0; k < n; k++)
            {
                int? turn = GetTurn(anglesDeg[k], transfiniteAngleDeg);
                if (!turn.HasValue) return null;
                turns[k] = turn.Value;
                if (turns[k] == 1) numOfEnds++;
                else if (turns[k] == -1) numOfCorners++;
            }
            if (numOfEnds - numOfCorners != 4 || numOfCorners == 0) return null;
            // The direction of edge k turns from that of edge k - 1 at vertex k
            int[] directions = new int[n];
            for (int k = 1; k < n; k++) directions[k] = (directions[k - 1] + turns[k] + 4) % 4;
            //
            SubmapFace submapFace = new SubmapFace(faceId, loopVertexIds, loopEdgeIds, directions, anglesDeg);
            if (IsGridPoor(mesh, vis, submapFace)) return null;
            return submapFace;
        }
        /// <summary>
        /// The interior angle of the face at every vertex of its loop: the sum of the angles of its cells at the vertex
        /// node. Null when a vertex is in no cell of the face.
        /// </summary>
        private static double[] GetInteriorAnglesDeg(FeMesh mesh, VisualizationData vis, int faceId, int[] vertexIds)
        {
            Dictionary<int, int> vertexIdIndex = new Dictionary<int, int>();
            for (int k = 0; k < vertexIds.Length; k++) vertexIdIndex[vertexIds[k]] = k;
            double[] anglesDeg = new double[vertexIds.Length];
            bool[] found = new bool[vertexIds.Length];
            int index;
            foreach (var cellId in vis.CellIdsByFace[faceId])
            {
                int[] cell = vis.Cells[cellId];
                int numOfCorners = cell.Length == 6 ? 3 : cell.Length == 8 ? 4 : cell.Length;
                for (int c = 0; c < numOfCorners; c++)
                {
                    if (!vertexIdIndex.TryGetValue(cell[c], out index)) continue;
                    double[] p = mesh.Nodes[cell[c]].Coor;
                    double[] previous = mesh.Nodes[cell[(c + numOfCorners - 1) % numOfCorners]].Coor;
                    double[] next = mesh.Nodes[cell[(c + 1) % numOfCorners]].Coor;
                    double angleDeg = GetAngleDeg(new double[] { previous[0] - p[0], previous[1] - p[1], previous[2] - p[2] },
                                                  new double[] { next[0] - p[0], next[1] - p[1], next[2] - p[2] });
                    if (double.IsNaN(angleDeg)) continue;
                    anglesDeg[index] += angleDeg;
                    found[index] = true;
                }
            }
            for (int k = 0; k < found.Length; k++)
            {
                if (!found[k]) return null;
            }
            return anglesDeg;
        }
        /// <summary>
        /// Whether the grid SubmapMapping lays over the face, in the plane of its boundary loop and with counts in
        /// proportion to the lengths of its edges, folds or has a quad corner below MinScaledJacobian, or whether no
        /// counts in proportion to the lengths give a valid outline. A face that turns back over the plane is not
        /// judged, as the grid in the plane would not be the one on the surface.
        /// </summary>
        private static bool IsGridPoor(FeMesh mesh, VisualizationData vis, SubmapFace face)
        {
            int n = face.EdgeIds.Length;
            List<double[]>[] polylines = new List<double[]>[n];
            double[] lengths = new double[n];
            for (int k = 0; k < n; k++)
            {
                polylines[k] = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, face.EdgeIds[k], face.VertexNodeIds[k]);
                if (polylines[k] == null) return false;
                TransfiniteFaceDetector.Resample(polylines[k], 1, out lengths[k]);
                if (!(lengths[k] > 0)) return true;
            }
            int[] counts = GetProportionalCounts(face.EdgeDirections, lengths);
            if (counts == null) return true;
            //
            double[][][] edgeXyz = new double[n][][];
            double length;
            for (int k = 0; k < n; k++) edgeXyz[k] = TransfiniteFaceDetector.Resample(polylines[k], counts[k], out length);
            // The plane of the boundary loop by Newell's method
            double[] normal = new double[3];
            double[] centre = new double[3];
            int numOfPoints = 0;
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < counts[k]; i++)
                {
                    double[] a = edgeXyz[k][i];
                    double[] b = edgeXyz[k][i + 1];
                    normal[0] += (a[1] - b[1]) * (a[2] + b[2]);
                    normal[1] += (a[2] - b[2]) * (a[0] + b[0]);
                    normal[2] += (a[0] - b[0]) * (a[1] + b[1]);
                    for (int d = 0; d < 3; d++) centre[d] += a[d];
                    numOfPoints++;
                }
            }
            double normalLength = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            if (normalLength == 0) return false;
            for (int d = 0; d < 3; d++)
            {
                normal[d] /= normalLength;
                centre[d] /= numOfPoints;
            }
            if (!TransfiniteFaceDetector.IsHeightFieldOverPlane(mesh, vis, face.FaceId, normal)) return false;
            //
            double[] axis1 = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double along = axis1[0] * normal[0] + axis1[1] * normal[1] + axis1[2] * normal[2];
            for (int d = 0; d < 3; d++) axis1[d] -= along * normal[d];
            double axis1Length = Math.Sqrt(axis1[0] * axis1[0] + axis1[1] * axis1[1] + axis1[2] * axis1[2]);
            for (int d = 0; d < 3; d++) axis1[d] /= axis1Length;
            double[] axis2 = new double[] { normal[1] * axis1[2] - normal[2] * axis1[1],
                                            normal[2] * axis1[0] - normal[0] * axis1[2],
                                            normal[0] * axis1[1] - normal[1] * axis1[0] };
            double[][][] edgeUv = new double[n][][];
            for (int k = 0; k < n; k++)
            {
                edgeUv[k] = new double[edgeXyz[k].Length][];
                for (int i = 0; i < edgeXyz[k].Length; i++)
                {
                    double[] point = edgeXyz[k][i];
                    double[] r = new double[] { point[0] - centre[0], point[1] - centre[1], point[2] - centre[2] };
                    edgeUv[k][i] = new double[] { r[0] * axis1[0] + r[1] * axis1[1] + r[2] * axis1[2],
                                                  r[0] * axis2[0] + r[1] * axis2[1] + r[2] * axis2[2] };
                }
            }
            SubmapGrid grid;
            try
            {
                grid = SubmapMapping.Map(face.EdgeDirections, edgeUv, edgeXyz, null);
            }
            catch (CaeException)
            {
                return true;
            }
            return grid.NumberOfFoldedQuads > 0 || grid.MinScaledJacobian < MinScaledJacobian;
        }
        /// <summary>
        /// The ideal rectilinear outline of a loop whose edge k runs in direction directions[k] and is lengths[k] long:
        /// the position of every loop vertex, vertex k being where edge k starts and vertex 0 at the origin. The edges
        /// along each direction are scaled to the mean of the two opposite sums, so that the outline closes. Null when a
        /// direction carries no length.
        /// </summary>
        public static double[][] GetIdealOutline(int[] directions, double[] lengths)
        {
            int n = directions.Length;
            double[] sums = new double[4];
            for (int k = 0; k < n; k++) sums[directions[k]] += lengths[k];
            double[] scales = new double[4];
            for (int d = 0; d < 4; d++)
            {
                if (!(sums[d] > 0)) return null;
                scales[d] = 0.5 * (sums[d] + sums[(d + 2) % 4]) / sums[d];
            }
            //
            double[][] points = new double[n][];
            double x = 0;
            double y = 0;
            for (int k = 0; k < n; k++)
            {
                points[k] = new double[] { x, y };
                double length = lengths[k] * scales[directions[k]];
                x += DirectionI[directions[k]] * length;
                y += DirectionJ[directions[k]] * length;
            }
            return points;
        }
        /// <summary>
        /// Numbers of elements of the edges in proportion to their lengths that close a valid logical outline: the
        /// ideal outline of GetIdealOutline with its vertices snapped to a lattice of a step of a fraction of its
        /// perimeter, halved while an edge snaps to nothing. Null when no step gives a valid outline.
        /// </summary>
        public static int[] GetProportionalCounts(int[] directions, double[] lengths)
        {
            int n = directions.Length;
            double[][] outline = GetIdealOutline(directions, lengths);
            if (outline == null) return null;
            double perimeter = 0;
            for (int k = 0; k < n; k++)
            {
                double[] a = outline[k];
                double[] b = outline[(k + 1) % n];
                perimeter += Math.Abs(b[0] - a[0]) + Math.Abs(b[1] - a[1]);
            }
            //
            int divisions = NumOfOutlineDivisions;
            for (int refinement = 0; refinement <= NumOfOutlineRefinements; refinement++)
            {
                double step = perimeter / divisions;
                long snappedX = 0;
                long snappedY = 0;
                int[] counts = new int[n];
                bool valid = true;
                for (int k = 0; k < n && valid; k++)
                {
                    // The last vertex is the first, at the origin
                    double[] next = outline[(k + 1) % n];
                    long nextX = (long)Math.Round(next[0] / step);
                    long nextY = (long)Math.Round(next[1] / step);
                    long count = DirectionI[directions[k]] * (nextX - snappedX) + DirectionJ[directions[k]] * (nextY - snappedY);
                    if (count < 1 || count > int.MaxValue) valid = false;
                    else counts[k] = (int)count;
                    if (directions[k] % 2 == 0) nextY = snappedY;
                    else nextX = snappedX;
                    snappedX = nextX;
                    snappedY = nextY;
                }
                string reason;
                if (valid && SubmapMapping.AreCountsValid(directions, counts, out reason)) return counts;
                divisions *= 2;
            }
            return null;
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
