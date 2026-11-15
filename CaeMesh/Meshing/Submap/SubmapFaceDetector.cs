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
    /// A face this leaves out, by its sum, by having no corner or by its grid, is classified again by the turns of its
    /// vertices and of its edges, so that a fillet, which is tangent at both ends and turns inside, and a chamfer,
    /// whose two vertices each turn by half a corner, are taken. A vertex turns by 180° less its interior angle and
    /// starts from the assignment above; an edge turns by the sum of that over the inner nodes of its polyline and
    /// starts from 0. While the assignments do not add up to four, the feature whose turn is furthest past its
    /// assignment in the direction needed changes by one step; a feature changes once at most, stays within -1 to 1,
    /// and may then be off its turn by no more than MaxTurnDeviationDeg, or the face is left free. Features within
    /// TieToleranceDeg of each other at the cut-off, the two vertices of a chamfer, give alternatives, which are judged
    /// by their grids. An edge assigned a turn turns in its middle (SubmapFace). A face classified this way needs no
    /// corner and no six edges: a rounded rectangle and a five-sided house are submapped. A half circle, the end of a
    /// slot, is off a turn of 90° by more than the limit and is not.
    /// A face that is a height field over the plane of its boundary loop is judged by the grid SubmapMapping lays over
    /// it in that plane, with counts in proportion to the lengths of its edges, and is left free when that grid folds
    /// or is poor; the meshers judge the grid of the counts they give it again. The corner of the one quad at the middle
    /// of a convex turn edge is left out of that judgement: its angle depends only on the number of elements along the
    /// edge, which the proportional counts set far finer than a mesh does.
    /// </summary>
    public static class SubmapFaceDetector
    {
        // Variables                                                                                                                
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of a submapped face; a face whose grid has a worse one is meshed
        /// free.
        /// </summary>
        public const double MinScaledJacobian = 0.2;
        /// <summary>
        /// How far the turn of a vertex or an edge may be off the turn its changed assignment gives it.
        /// </summary>
        public const double MaxTurnDeviationDeg = 50;
        private const double TieToleranceDeg = 1;
        private const int MaxNumOfAlternatives = 6;
        private const int MaxDeficit = 4;
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
            if (faceEdgeIds == null || faceEdgeIds.Length < 2 || faceId >= vis.CellIdsByFace.Length) return null;
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
            if (n >= 6 && numOfEnds - numOfCorners == 4 && numOfCorners > 0)
            {
                SubmapFace submapFace = new SubmapFace(faceId, loopVertexIds, loopEdgeIds, GetDirections(turns, null),
                                                       anglesDeg, null);
                if (!IsGridPoor(mesh, vis, submapFace)) return submapFace;
            }
            return DetectFaceByTurns(mesh, vis, faceId, loopVertexIds, loopEdgeIds, anglesDeg, turns);
        }
        /// <summary>
        /// Classifies the vertices and the edges of the loop by their turns, starting from the assignments of the
        /// vertices by their angles; null when no assignment adds up to four within the limits, when the assignments
        /// already do, and when the grid of the best one is poor.
        /// </summary>
        private static SubmapFace DetectFaceByTurns(FeMesh mesh, VisualizationData vis, int faceId, int[] loopVertexIds,
                                                    int[] loopEdgeIds, double[] anglesDeg, int[] vertexTurns)
        {
            int n = loopEdgeIds.Length;
            int deficit = 4;
            for (int k = 0; k < n; k++) deficit -= vertexTurns[k];
            if (deficit == 0 || Math.Abs(deficit) > MaxDeficit) return null;
            double[] edgeTurnsDeg = GetEdgeTurnsDeg(mesh, vis, faceId, loopVertexIds, loopEdgeIds);
            if (edgeTurnsDeg == null) return null;
            // Feature 2k is vertex k and feature 2k + 1 edge k, in loop order
            double[] turnsDeg = new double[2 * n];
            int[] assignments = new int[2 * n];
            for (int k = 0; k < n; k++)
            {
                turnsDeg[2 * k] = 180 - anglesDeg[k];
                assignments[2 * k] = vertexTurns[k];
                turnsDeg[2 * k + 1] = edgeTurnsDeg[k];
            }
            int step = Math.Sign(deficit);
            int numOfSteps = Math.Abs(deficit);
            // The features that may change, by how far their turns are past their assignments in the direction needed
            List<int> candidates = new List<int>();
            double[] scores = new double[2 * n];
            for (int f = 0; f < 2 * n; f++)
            {
                int changed = assignments[f] + step;
                if (changed < -1 || changed > 1 || Math.Abs(turnsDeg[f] - 90 * changed) > MaxTurnDeviationDeg) continue;
                scores[f] = step * (turnsDeg[f] - 90 * assignments[f]);
                candidates.Add(f);
            }
            if (candidates.Count < numOfSteps) return null;
            candidates.Sort((a, b) => scores[a] != scores[b] ? scores[b].CompareTo(scores[a]) : a.CompareTo(b));
            double cutOff = scores[candidates[numOfSteps - 1]];
            List<int> taken = new List<int>();
            List<int> tied = new List<int>();
            foreach (var f in candidates)
            {
                if (scores[f] > cutOff + TieToleranceDeg) taken.Add(f);
                else if (scores[f] >= cutOff - TieToleranceDeg) tied.Add(f);
            }
            tied.Sort();
            //
            SubmapFace best = null;
            double bestQuality = double.NegativeInfinity;
            SubmapFace firstNotJudged = null;
            foreach (var choice in GetCombinations(tied.Count, numOfSteps - taken.Count, MaxNumOfAlternatives))
            {
                int[] changedAssignments = (int[])assignments.Clone();
                foreach (var f in taken) changedAssignments[f] += step;
                foreach (var i in choice) changedAssignments[tied[i]] += step;
                int[] changedVertexTurns = new int[n];
                int[] edgeTurns = new int[n];
                bool hasTurnEdges = false;
                for (int k = 0; k < n; k++)
                {
                    changedVertexTurns[k] = changedAssignments[2 * k];
                    edgeTurns[k] = changedAssignments[2 * k + 1];
                    if (edgeTurns[k] != 0) hasTurnEdges = true;
                }
                if (!hasTurnEdges) edgeTurns = null;
                SubmapFace face = new SubmapFace(faceId, loopVertexIds, loopEdgeIds, GetDirections(changedVertexTurns, edgeTurns),
                                                 anglesDeg, edgeTurns);
                double quality = GetGridQuality(mesh, vis, face);
                if (double.IsNaN(quality))
                {
                    if (firstNotJudged == null) firstNotJudged = face;
                }
                else if (quality > bestQuality)
                {
                    best = face;
                    bestQuality = quality;
                }
            }
            if (best != null && bestQuality >= MinScaledJacobian) return best;
            return firstNotJudged;
        }
        /// <summary>
        /// The direction of every edge, edge k turning from edge k - 1 by the turn of edge k - 1 in its middle and by
        /// the turn of vertex k, edge 0 running along +i.
        /// </summary>
        private static int[] GetDirections(int[] vertexTurns, int[] edgeTurns)
        {
            int n = vertexTurns.Length;
            int[] directions = new int[n];
            for (int k = 1; k < n; k++)
            {
                int edgeTurn = edgeTurns == null ? 0 : edgeTurns[k - 1];
                directions[k] = (directions[k - 1] + edgeTurn + vertexTurns[k] + 8) % 4;
            }
            return directions;
        }
        /// <summary>
        /// The choices of count indices out of numOfItems in lexicographic order, at most maxNumOfChoices of them.
        /// </summary>
        private static List<int[]> GetCombinations(int numOfItems, int count, int maxNumOfChoices)
        {
            List<int[]> combinations = new List<int[]>();
            if (count < 0 || count > numOfItems) return combinations;
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            while (combinations.Count < maxNumOfChoices)
            {
                combinations.Add((int[])indices.Clone());
                int position = count - 1;
                while (position >= 0 && indices[position] == numOfItems - count + position) position--;
                if (position < 0) break;
                indices[position]++;
                for (int i = position + 1; i < count; i++) indices[i] = indices[i - 1] + 1;
            }
            return combinations;
        }
        /// <summary>
        /// The interior angle of the face at every vertex of its loop: the sum of the angles of its cells at the vertex
        /// node. Null when a vertex is in no cell of the face.
        /// </summary>
        private static double[] GetInteriorAnglesDeg(FeMesh mesh, VisualizationData vis, int faceId, int[] vertexIds)
        {
            Dictionary<int, int> vertexIdIndex = new Dictionary<int, int>();
            for (int k = 0; k < vertexIds.Length; k++) vertexIdIndex[vertexIds[k]] = k;
            bool[] found;
            double[] anglesDeg = GetNodeAnglesDeg(mesh, vis, faceId, vertexIdIndex, vertexIds.Length, out found);
            for (int k = 0; k < found.Length; k++)
            {
                if (!found[k]) return null;
            }
            return anglesDeg;
        }
        /// <summary>
        /// The turn of every edge of the loop: the sum of 180° less the interior angle of the face over the inner
        /// nodes of its polyline that are corners of its cells. Null when an edge does not chain from its vertex.
        /// </summary>
        private static double[] GetEdgeTurnsDeg(FeMesh mesh, VisualizationData vis, int faceId, int[] loopVertexIds,
                                                int[] loopEdgeIds)
        {
            int n = loopEdgeIds.Length;
            Dictionary<int, int> nodeIdIndex = new Dictionary<int, int>();
            List<int> nodeEdgeIndices = new List<int>();
            for (int k = 0; k < n; k++)
            {
                List<int> nodeIds = GetEdgeNodeIds(vis, loopEdgeIds[k], loopVertexIds[k]);
                if (nodeIds == null) return null;
                for (int i = 1; i < nodeIds.Count - 1; i++)
                {
                    if (nodeIdIndex.ContainsKey(nodeIds[i])) continue;
                    nodeIdIndex.Add(nodeIds[i], nodeEdgeIndices.Count);
                    nodeEdgeIndices.Add(k);
                }
            }
            bool[] found;
            double[] anglesDeg = GetNodeAnglesDeg(mesh, vis, faceId, nodeIdIndex, nodeEdgeIndices.Count, out found);
            double[] turnsDeg = new double[n];
            for (int i = 0; i < nodeEdgeIndices.Count; i++)
            {
                if (found[i]) turnsDeg[nodeEdgeIndices[i]] += 180 - anglesDeg[i];
            }
            return turnsDeg;
        }
        /// <summary>
        /// The sum of the angles of the cells of the face at the given nodes, by the index the nodes map to, and
        /// whether a node is a corner of a cell at all.
        /// </summary>
        private static double[] GetNodeAnglesDeg(FeMesh mesh, VisualizationData vis, int faceId,
                                                 Dictionary<int, int> nodeIdIndex, int numOfIndices, out bool[] found)
        {
            double[] anglesDeg = new double[numOfIndices];
            found = new bool[numOfIndices];
            int index;
            foreach (var cellId in vis.CellIdsByFace[faceId])
            {
                int[] cell = vis.Cells[cellId];
                int numOfCorners = cell.Length == 6 ? 3 : cell.Length == 8 ? 4 : cell.Length;
                for (int c = 0; c < numOfCorners; c++)
                {
                    if (!nodeIdIndex.TryGetValue(cell[c], out index)) continue;
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
            return anglesDeg;
        }
        /// <summary>
        /// The nodes of an edge in order from the given vertex, read off its edge cells as
        /// TransfiniteFaceDetector.GetEdgePoints reads its points. Null when the cells do not chain from the vertex.
        /// </summary>
        private static List<int> GetEdgeNodeIds(VisualizationData vis, int edgeId, int startNodeId)
        {
            int[] cellIds = vis.EdgeCellIdsByEdge[edgeId];
            bool[] used = new bool[cellIds.Length];
            List<int> nodeIds = new List<int> { startNodeId };
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
                nodeIds.Add(next);
                current = next;
            }
            return nodeIds;
        }
        /// <summary>
        /// Whether the grid SubmapMapping lays over the face, in the plane of its boundary loop and with counts in
        /// proportion to the lengths of its edges, folds or has a quad corner below MinScaledJacobian, or whether no
        /// counts in proportion to the lengths give a valid outline. A face that turns back over the plane is not
        /// judged, as the grid in the plane would not be the one on the surface.
        /// </summary>
        private static bool IsGridPoor(FeMesh mesh, VisualizationData vis, SubmapFace face)
        {
            double quality = GetGridQuality(mesh, vis, face);
            return !double.IsNaN(quality) && quality < MinScaledJacobian;
        }
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of the grid of IsGridPoor, over the logical loop of the face,
        /// a turn edge split at half its length. The corner at the middle of a convex turn edge is left out. -1 when
        /// the grid folds or cannot be made, NaN when it is not judged.
        /// </summary>
        private static double GetGridQuality(FeMesh mesh, VisualizationData vis, SubmapFace face)
        {
            List<List<double[]>> polylineList = new List<List<double[]>>();
            List<int> convexMiddleEdges = new List<int>();
            for (int k = 0; k < face.EdgeIds.Length; k++)
            {
                List<double[]> polyline = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, face.EdgeIds[k],
                                                                                face.VertexNodeIds[k]);
                if (polyline == null) return double.NaN;
                if (face.GetEdgeTurn(k) == 0) polylineList.Add(polyline);
                else
                {
                    List<double[]> firstHalf;
                    List<double[]> secondHalf;
                    if (!SplitAtHalfLength(polyline, out firstHalf, out secondHalf)) return -1;
                    polylineList.Add(firstHalf);
                    if (face.GetEdgeTurn(k) > 0) convexMiddleEdges.Add(polylineList.Count);
                    polylineList.Add(secondHalf);
                }
            }
            int n = polylineList.Count;
            List<double[]>[] polylines = polylineList.ToArray();
            int[] directions = face.LogicalEdgeDirections;
            double[] lengths = new double[n];
            for (int k = 0; k < n; k++)
            {
                TransfiniteFaceDetector.Resample(polylines[k], 1, out lengths[k]);
                if (!(lengths[k] > 0)) return -1;
            }
            int[] counts = GetProportionalCounts(directions, lengths);
            if (counts == null) return -1;
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
            if (normalLength == 0) return double.NaN;
            for (int d = 0; d < 3; d++)
            {
                normal[d] /= normalLength;
                centre[d] /= numOfPoints;
            }
            if (!TransfiniteFaceDetector.IsHeightFieldOverPlane(mesh, vis, face.FaceId, normal)) return double.NaN;
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
                grid = SubmapMapping.Map(directions, edgeUv, edgeXyz, null);
            }
            catch (CaeException)
            {
                return -1;
            }
            if (grid.NumberOfFoldedQuads > 0) return -1;
            if (convexMiddleEdges.Count == 0) return grid.MinScaledJacobian;
            // The grid again in the plane, which the frame of the mapping only scales and shears a little, without the
            // corners at the middles of the convex turn edges
            int numOfBoundaryNodes = grid.NumberOfBoundaryNodes;
            double[][] points = new double[numOfBoundaryNodes + grid.NumberOfInnerNodes][];
            for (int k = 0; k < n; k++)
            {
                for (int i = 0; i < counts[k]; i++) points[grid.GetBoundaryNodeId(k, i)] = edgeUv[k][i];
            }
            for (int i = 0; i < grid.NumberOfInnerNodes; i++) points[numOfBoundaryNodes + i] = grid.InnerUv[i];
            HashSet<int> leftOutNodeIds = new HashSet<int>();
            foreach (var k in convexMiddleEdges) leftOutNodeIds.Add(grid.GetBoundaryNodeId(k, 0));
            double orientation = ThreeBlockMapping.GetSignedArea(points, numOfBoundaryNodes) < 0 ? -1 : 1;
            double minScaledJacobian = 1;
            foreach (var quad in grid.Quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (leftOutNodeIds.Contains(quad[c])) continue;
                    minScaledJacobian = Math.Min(minScaledJacobian,
                                                 ThreeBlockMapping.GetScaledJacobian(points, quad, c, orientation));
                }
            }
            return minScaledJacobian;
        }
        /// <summary>
        /// The polyline split at half its length into two, the point there ending the first and starting the second.
        /// False when it has no length.
        /// </summary>
        private static bool SplitAtHalfLength(List<double[]> polyline, out List<double[]> firstHalf,
                                              out List<double[]> secondHalf)
        {
            firstHalf = null;
            secondHalf = null;
            double[] lengths = new double[polyline.Count];
            for (int i = 1; i < polyline.Count; i++)
            {
                double[] a = polyline[i - 1];
                double[] b = polyline[i];
                lengths[i] = lengths[i - 1] + Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) +
                                                        (b[2] - a[2]) * (b[2] - a[2]));
            }
            double half = 0.5 * lengths[polyline.Count - 1];
            if (!(half > 0)) return false;
            //
            int segment = 1;
            while (segment < polyline.Count - 1 && lengths[segment] < half) segment++;
            double span = lengths[segment] - lengths[segment - 1];
            double f = span > 0 ? Math.Max(0, Math.Min(1, (half - lengths[segment - 1]) / span)) : 0;
            double[] p = polyline[segment - 1];
            double[] q = polyline[segment];
            double[] middle = new double[] { p[0] + f * (q[0] - p[0]), p[1] + f * (q[1] - p[1]), p[2] + f * (q[2] - p[2]) };
            firstHalf = new List<double[]>();
            for (int i = 0; i < segment; i++) firstHalf.Add(polyline[i]);
            firstHalf.Add(middle);
            secondHalf = new List<double[]> { middle };
            for (int i = segment; i < polyline.Count; i++) secondHalf.Add(polyline[i]);
            return true;
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
