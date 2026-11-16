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
    /// assignment in the direction needed changes by one step; a vertex changes once at most and stays within -1 to 1,
    /// an edge may take two steps and stays within -2 to 2, its second step ranked by what is left of its turn after
    /// the first, so that a half circle, the end of a slot, turns twice. Every feature that changed may then be off its
    /// turn by no more than MaxTurnDeviationDeg, or the alternative is not valid: an edge of 180° that took one step
    /// is not. Features within TieToleranceDeg of each other at the cut-off, the two vertices of a chamfer, give
    /// alternatives, which are judged by their grids. An edge assigned a turn turns in its middle, one assigned two
    /// turns at a quarter and three quarters of its length (SubmapFace). A face classified this way needs no corner and
    /// no six edges: a rounded rectangle, a five-sided house and a slot-shaped plate are submapped.
    /// A face that is a height field over the plane of its boundary loop is judged by the grid SubmapMapping lays over
    /// it in that plane, with counts in proportion to the lengths of its edges, and is left free when that grid folds
    /// or is poor; the meshers judge the grid of the counts they give it again. The corner of the one quad at every
    /// inner corner of a convex turn edge, its middle or the two of an edge that turns twice, is left out of that
    /// judgement: its angle, 180° - 90°/n or 180° - 180°/n for n elements along the edge, depends only on the count,
    /// which the proportional counts set far finer than a mesh does.
    /// A face of several boundary loops is submapped when its holes have corners. Its loops are traversed with the face
    /// on the same side, by the winding of a cell of the face along the first segment of every loop, and the turn of a
    /// loop, the sum of the turns of its vertices and of its edges, tells the outer loop, about +360°, from a hole,
    /// about -360°; there must be exactly one outer loop. Every loop is classified by the rules above, a hole with the
    /// target -4 instead of +4, so that a rectangular hole is four corners and a cut-out with four filleted corners
    /// four edges turning by -1; the outer loop then needs no corner and no six edges. A loop that cannot be classified
    /// leaves the face free. The directions of a hole are turned into the logical frame of the outer loop by the
    /// geometry: in the plane of the outer loop, the frame angle of a loop is the length-weighted circular mean of the
    /// angle of the chord of every logical edge less 90° times its direction, and the difference between the frame
    /// angles of the hole and of the outer loop, rounded to a multiple of 90°, is the turn. A difference further than
    /// MaxHoleTurnDeviationDeg from a multiple of 90°, a diamond-shaped hole, and a face that is not a height field
    /// over that plane leave the face free. The face is then judged like a face of one loop, all loops snapped with
    /// one lattice step that the outer loop sets, the holes placed by SubmapHolePlacement.
    /// A hole whose loop is a circle is round, which is decided before the rules above: every vertex a side by its
    /// angle, its edges of the same length within MaxRoundLengthDeviation, and every point of their polylines as far
    /// from the centre of the circle fitted through them in the plane as its radius within MaxRoundRadiusDeviation.
    /// A single closed edge, two half circles and four quarters are round; an ellipse is not. A round hole needs no
    /// directions: in the lattice it is a square whose corners SubmapHolePlacement puts where the lattice wants them.
    /// An outer loop that is round, a disc or an annulus, leaves the face free.
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
        /// <summary>
        /// How far the frame of a hole may be turned off a multiple of 90° from the frame of the outer loop.
        /// </summary>
        public const double MaxHoleTurnDeviationDeg = 20;
        /// <summary>
        /// How much the lengths of the edges of a round hole may differ, relative to the longest.
        /// </summary>
        public const double MaxRoundLengthDeviation = 0.01;
        /// <summary>
        /// How far a point of a round hole may lie from its circle, relative to its radius.
        /// </summary>
        public const double MaxRoundRadiusDeviation = 0.02;
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
                                                         out loopVertexIds))
                return DetectFaceWithHoles(mesh, vis, faceId, faceEdgeIds, transfiniteAngleDeg, edgeIdVertexIds);
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
            if (IsRoundLoop(mesh, vis, loopEdgeIds, loopVertexIds, anglesDeg, transfiniteAngleDeg)) return null;
            return DetectFaceByTurns(mesh, vis, faceId, loopVertexIds, loopEdgeIds, anglesDeg, turns);
        }
        /// <summary>
        /// Whether the one loop of a face is round (IsRoundLoop), a disc of half circles or quarters, which is left free.
        /// </summary>
        private static bool IsRoundLoop(FeMesh mesh, VisualizationData vis, int[] loopEdgeIds, int[] loopVertexIds,
                                        double[] anglesDeg, double transfiniteAngleDeg)
        {
            List<double[]>[] polylines = new List<double[]>[loopEdgeIds.Length];
            List<double[]> points = new List<double[]>();
            for (int k = 0; k < loopEdgeIds.Length; k++)
            {
                polylines[k] = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, loopEdgeIds[k], loopVertexIds[k]);
                if (polylines[k] == null) return false;
                for (int i = 0; i < polylines[k].Count - 1; i++) points.Add(polylines[k][i]);
            }
            double[][] frame = GetPlaneFrame(points);
            return frame != null && IsRoundLoop(anglesDeg, polylines, frame, transfiniteAngleDeg);
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
            List<int[]> alternatives = GetChangedAssignments(anglesDeg, edgeTurnsDeg, vertexTurns, deficit);
            if (alternatives == null) return null;
            //
            SubmapFace best = null;
            double bestQuality = double.NegativeInfinity;
            SubmapFace firstNotJudged = null;
            foreach (var changedAssignments in alternatives)
            {
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
        /// The assignments of the vertices and the edges of a loop, feature 2k vertex k and feature 2k + 1 edge k, that
        /// change the assignments of the vertices by their angles by the deficit, one step at a time, the steps whose
        /// features have turns furthest past their assignments before the step in the direction needed; a vertex takes
        /// one step at most and an edge two, the second ranked by the turn left after the first. Steps tied at the
        /// cut-off give alternatives; an alternative in which a feature that changed is off its turn by more than
        /// MaxTurnDeviationDeg is left out. Null when too few steps may be taken or no alternative is left.
        /// </summary>
        private static List<int[]> GetChangedAssignments(double[] anglesDeg, double[] edgeTurnsDeg, int[] vertexTurns,
                                                         int deficit)
        {
            int n = vertexTurns.Length;
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
            // The steps that may be taken, by how far the turns of their features are past their assignments before them
            // in the direction needed
            List<int> features = new List<int>();
            List<double> scores = new List<double>();
            for (int f = 0; f < 2 * n; f++)
            {
                int limit = f % 2 == 0 ? 1 : 2;
                for (int s = 0; s < limit; s++)
                {
                    int changed = assignments[f] + (s + 1) * step;
                    if (changed < -limit || changed > limit) break;
                    features.Add(f);
                    scores.Add(step * (turnsDeg[f] - 90 * (assignments[f] + s * step)));
                }
            }
            if (features.Count < numOfSteps) return null;
            List<int> candidates = new List<int>();
            for (int c = 0; c < features.Count; c++) candidates.Add(c);
            candidates.Sort((a, b) => scores[a] != scores[b] ? scores[b].CompareTo(scores[a]) : a.CompareTo(b));
            double cutOff = scores[candidates[numOfSteps - 1]];
            List<int> taken = new List<int>();
            List<int> tied = new List<int>();
            foreach (var c in candidates)
            {
                if (scores[c] > cutOff + TieToleranceDeg) taken.Add(c);
                else if (scores[c] >= cutOff - TieToleranceDeg) tied.Add(c);
            }
            tied.Sort();
            //
            List<int[]> alternatives = new List<int[]>();
            foreach (var choice in GetCombinations(tied.Count, numOfSteps - taken.Count, MaxNumOfAlternatives))
            {
                int[] changedAssignments = (int[])assignments.Clone();
                foreach (var c in taken) changedAssignments[features[c]] += step;
                foreach (var i in choice) changedAssignments[features[tied[i]]] += step;
                bool valid = true;
                for (int f = 0; f < 2 * n && valid; f++)
                {
                    if (changedAssignments[f] != assignments[f] &&
                        Math.Abs(turnsDeg[f] - 90 * changedAssignments[f]) > MaxTurnDeviationDeg) valid = false;
                }
                if (valid) alternatives.Add(changedAssignments);
            }
            return alternatives.Count > 0 ? alternatives : null;
        }
        /// <summary>
        /// The face of several boundary loops whose holes have corners or are round, its loops classified as round, by
        /// their vertices or by their turns, the outer loop first, and the directions of its holes turned into the frame
        /// of the outer loop; null when a loop cannot be classified, the outer loop is round, the loops are not one outer
        /// loop and its holes, a hole is turned off a multiple of 90°, the face is not a height field over the plane of
        /// its outer loop, or its grid is poor.
        /// </summary>
        private static SubmapFace DetectFaceWithHoles(FeMesh mesh, VisualizationData vis, int faceId, int[] faceEdgeIds,
                                                      double transfiniteAngleDeg, Dictionary<int, int[]> edgeIdVertexIds)
        {
            List<int[]> loopsEdgeIds;
            List<int[]> loopsVertexIds;
            if (!GetBoundaryLoops(vis, faceEdgeIds, edgeIdVertexIds, out loopsEdgeIds, out loopsVertexIds) ||
                loopsEdgeIds.Count < 2) return null;
            int numOfLoops = loopsEdgeIds.Count;
            // Every loop with the face on its left
            for (int l = 0; l < numOfLoops; l++)
            {
                int side = GetFaceSide(vis, faceId, loopsEdgeIds[l][0], loopsVertexIds[l][0]);
                if (side == 0) return null;
                if (side > 0) continue;
                int n = loopsEdgeIds[l].Length;
                int[] edgeIds = new int[n];
                int[] vertexIds = new int[n];
                for (int k = 0; k < n; k++)
                {
                    edgeIds[k] = loopsEdgeIds[l][n - 1 - k];
                    vertexIds[k] = loopsVertexIds[l][(n - k) % n];
                }
                loopsEdgeIds[l] = edgeIds;
                loopsVertexIds[l] = vertexIds;
            }
            // The angles and the turns of every loop, the outer loop turning by about +360° and a hole by about -360°
            Dictionary<int, int> vertexIdIndex = new Dictionary<int, int>();
            foreach (var vertexIds in loopsVertexIds)
            {
                foreach (var vertexId in vertexIds) vertexIdIndex[vertexId] = vertexIdIndex.Count;
            }
            bool[] found;
            double[] allAnglesDeg = GetNodeAnglesDeg(mesh, vis, faceId, vertexIdIndex, vertexIdIndex.Count, out found);
            for (int i = 0; i < found.Length; i++)
            {
                if (!found[i]) return null;
            }
            double[][] anglesDeg = new double[numOfLoops][];
            double[][] edgeTurnsDeg = new double[numOfLoops][];
            int outer = -1;
            for (int l = 0; l < numOfLoops; l++)
            {
                int n = loopsEdgeIds[l].Length;
                anglesDeg[l] = new double[n];
                for (int k = 0; k < n; k++) anglesDeg[l][k] = allAnglesDeg[vertexIdIndex[loopsVertexIds[l][k]]];
                edgeTurnsDeg[l] = GetEdgeTurnsDeg(mesh, vis, faceId, loopsVertexIds[l], loopsEdgeIds[l]);
                if (edgeTurnsDeg[l] == null) return null;
                double loopTurnDeg = 0;
                for (int k = 0; k < n; k++) loopTurnDeg += 180 - anglesDeg[l][k] + edgeTurnsDeg[l][k];
                if (loopTurnDeg > 180)
                {
                    if (outer >= 0) return null;
                    outer = l;
                }
                else if (loopTurnDeg > -180) return null;
            }
            if (outer < 0) return null;
            int[] order = new int[numOfLoops];
            order[0] = outer;
            for (int l = 0, o = 1; l < numOfLoops; l++)
            {
                if (l != outer) order[o++] = l;
            }
            // The plane of the outer loop by Newell's method over the points of its edges
            List<double[]>[][] polylines = new List<double[]>[numOfLoops][];
            for (int o = 0; o < numOfLoops; o++)
            {
                int l = order[o];
                polylines[o] = new List<double[]>[loopsEdgeIds[l].Length];
                for (int k = 0; k < polylines[o].Length; k++)
                {
                    polylines[o][k] = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, loopsEdgeIds[l][k], loopsVertexIds[l][k]);
                    if (polylines[o][k] == null) return null;
                }
            }
            List<double[]> outerPoints = new List<double[]>();
            foreach (var polyline in polylines[0])
            {
                for (int i = 0; i < polyline.Count - 1; i++) outerPoints.Add(polyline[i]);
            }
            double[][] frame = GetPlaneFrame(outerPoints);
            if (frame == null || !TransfiniteFaceDetector.IsHeightFieldOverPlane(mesh, vis, faceId, frame[1])) return null;
            // The classifications of every loop in its own frame, edge 0 along +i, a round hole before the other rules;
            // a round outer loop leaves the face free
            List<int[][]>[] assignments = new List<int[][]>[numOfLoops];
            bool[] round = new bool[numOfLoops];
            for (int o = 0; o < numOfLoops; o++)
            {
                int l = order[o];
                round[o] = IsRoundLoop(anglesDeg[l], polylines[o], frame, transfiniteAngleDeg);
                if (round[o] && o == 0) return null;
                if (round[o])
                {
                    assignments[o] = new List<int[][]> { new int[][] { new int[loopsEdgeIds[l].Length], null } };
                    continue;
                }
                assignments[o] = GetLoopAssignments(anglesDeg[l], edgeTurnsDeg[l], o == 0 ? 4 : -4, transfiniteAngleDeg);
                if (assignments[o].Count == 0) return null;
            }
            //
            SubmapFace best = null;
            double bestQuality = double.NegativeInfinity;
            int[] choice = new int[numOfLoops];
            for (int alternative = 0; alternative < MaxNumOfAlternatives; alternative++)
            {
                int[][] outerAssignment = assignments[0][choice[0]];
                double outerAngle = GetFrameAngle(polylines[0], outerAssignment[0], outerAssignment[1], frame);
                SubmapFace[] holes = new SubmapFace[numOfLoops - 1];
                for (int o = 1; o < numOfLoops && holes != null; o++)
                {
                    int l = order[o];
                    int[][] assignment = assignments[o][choice[o]];
                    if (round[o])
                    {
                        holes[o - 1] = new SubmapFace(faceId, loopsVertexIds[l], loopsEdgeIds[l], assignment[0], anglesDeg[l],
                                                      null, null, true);
                        continue;
                    }
                    double angle = GetFrameAngle(polylines[o], assignment[0], assignment[1], frame);
                    double difference = (angle - outerAngle) * 180 / Math.PI;
                    double turn = Math.Round(difference / 90);
                    if (double.IsNaN(difference) || Math.Abs(difference - 90 * turn) > MaxHoleTurnDeviationDeg)
                    {
                        holes = null;
                        break;
                    }
                    int[] directions = new int[assignment[0].Length];
                    for (int k = 0; k < directions.Length; k++)
                        directions[k] = (int)(((assignment[0][k] + (long)turn) % 4 + 4) % 4);
                    holes[o - 1] = new SubmapFace(faceId, loopsVertexIds[l], loopsEdgeIds[l], directions, anglesDeg[l],
                                                  assignment[1]);
                }
                if (holes != null)
                {
                    SubmapFace face = new SubmapFace(faceId, loopsVertexIds[outer], loopsEdgeIds[outer], outerAssignment[0],
                                                     anglesDeg[outer], outerAssignment[1], holes);
                    double quality = GetGridQuality(mesh, vis, face, polylines, frame);
                    if (quality > bestQuality)
                    {
                        best = face;
                        bestQuality = quality;
                    }
                }
                // The next combination of the alternatives of the loops
                int position = numOfLoops - 1;
                while (position >= 0 && choice[position] == assignments[position].Count - 1)
                {
                    choice[position] = 0;
                    position--;
                }
                if (position < 0) break;
                choice[position]++;
            }
            if (best != null && bestQuality >= MinScaledJacobian) return best;
            return null;
        }
        /// <summary>
        /// The classifications of one loop of a face whose turns must add up to the target, +4 for the outer loop and
        /// -4 for a hole, each as its directions in its own frame, edge 0 along +i, and its edge turns, null for none:
        /// the one by the angles of its vertices when they add up, otherwise those by the turns of its vertices and of
        /// its edges. Empty when a vertex is no end, side or corner, or no assignment adds up within the limits.
        /// </summary>
        private static List<int[][]> GetLoopAssignments(double[] anglesDeg, double[] edgeTurnsDeg, int target,
                                                        double transfiniteAngleDeg)
        {
            List<int[][]> assignments = new List<int[][]>();
            int n = anglesDeg.Length;
            int[] vertexTurns = new int[n];
            int deficit = target;
            for (int k = 0; k < n; k++)
            {
                int? turn = GetTurn(anglesDeg[k], transfiniteAngleDeg);
                if (!turn.HasValue) return assignments;
                vertexTurns[k] = turn.Value;
                deficit -= turn.Value;
            }
            if (deficit == 0)
            {
                assignments.Add(new int[][] { GetDirections(vertexTurns, null), null });
                return assignments;
            }
            if (Math.Abs(deficit) > MaxDeficit) return assignments;
            List<int[]> alternatives = GetChangedAssignments(anglesDeg, edgeTurnsDeg, vertexTurns, deficit);
            if (alternatives == null) return assignments;
            foreach (var changedAssignments in alternatives)
            {
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
                assignments.Add(new int[][] { GetDirections(changedVertexTurns, edgeTurns), edgeTurns });
            }
            return assignments;
        }
        /// <summary>
        /// Whether a loop is a circle: every vertex a side by its angle, the polylines of its edges of the same length
        /// within MaxRoundLengthDeviation, and every point of them as far from the centre of the circle fitted through
        /// them in the plane of the frame as its radius within MaxRoundRadiusDeviation. The circle is the algebraic
        /// least squares fit, exact for points on a circle however they are spread.
        /// </summary>
        private static bool IsRoundLoop(double[] anglesDeg, List<double[]>[] polylines, double[][] frame,
                                        double transfiniteAngleDeg)
        {
            for (int k = 0; k < anglesDeg.Length; k++)
            {
                int? turn = GetTurn(anglesDeg[k], transfiniteAngleDeg);
                if (!turn.HasValue || turn.Value != 0) return false;
            }
            double minLength = double.MaxValue;
            double maxLength = 0;
            List<double[]> points = new List<double[]>();
            foreach (var polyline in polylines)
            {
                double length;
                TransfiniteFaceDetector.Resample(polyline, 1, out length);
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                for (int i = 0; i < polyline.Count - 1; i++) points.Add(ToPlane(frame, polyline[i]));
            }
            if (!(maxLength > 0) || maxLength - minLength > MaxRoundLengthDeviation * maxLength || points.Count < 3)
                return false;
            // The circle x² + y² + d x + e y + f = 0 through the points taken about their mean
            double[] mean = new double[2];
            foreach (var point in points)
            {
                mean[0] += point[0] / points.Count;
                mean[1] += point[1] / points.Count;
            }
            double sxx = 0;
            double sxy = 0;
            double syy = 0;
            double sxz = 0;
            double syz = 0;
            double sz = 0;
            foreach (var point in points)
            {
                double x = point[0] - mean[0];
                double y = point[1] - mean[1];
                double z = x * x + y * y;
                sxx += x * x;
                sxy += x * y;
                syy += y * y;
                sxz += x * z;
                syz += y * z;
                sz += z;
            }
            double determinant = sxx * syy - sxy * sxy;
            if (!(Math.Abs(determinant) > 0)) return false;
            double d = (-sxz * syy + syz * sxy) / determinant;
            double e = (-syz * sxx + sxz * sxy) / determinant;
            double f = -sz / points.Count;
            double[] centre = new double[] { mean[0] - 0.5 * d, mean[1] - 0.5 * e };
            double radiusSquared = 0.25 * (d * d + e * e) - f;
            if (!(radiusSquared > 0)) return false;
            double radius = Math.Sqrt(radiusSquared);
            foreach (var point in points)
            {
                double distance = Math.Sqrt((point[0] - centre[0]) * (point[0] - centre[0]) +
                                            (point[1] - centre[1]) * (point[1] - centre[1]));
                if (Math.Abs(distance - radius) > MaxRoundRadiusDeviation * radius) return false;
            }
            return true;
        }
        /// <summary>
        /// Orders the edges of a face head to tail into its boundary loops, each starting at the first edge of the face
        /// not yet in a loop, from the first vertex of that edge. A closed edge, which starts and ends at its one vertex,
        /// joins that vertex twice and is a loop of its own, a round hole of one edge. False when a vertex does not join
        /// exactly two edges or an edge is used twice: an edge inside the face or a seam.
        /// </summary>
        internal static bool GetBoundaryLoops(VisualizationData vis, int[] faceEdgeIds, Dictionary<int, int[]> edgeIdVertexIds,
                                             out List<int[]> loopsEdgeIds, out List<int[]> loopsVertexIds)
        {
            loopsEdgeIds = new List<int[]>();
            loopsVertexIds = new List<int[]>();
            int[] vertexIds;
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in faceEdgeIds)
            {
                if (!edgeIdVertexIds.TryGetValue(edgeId, out vertexIds))
                {
                    vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                    edgeIdVertexIds.Add(edgeId, vertexIds);
                }
                if (vertexIds.Length == 1)
                {
                    vertexIds = new int[] { vertexIds[0], vertexIds[0] };
                    edgeIdVertexIds[edgeId] = vertexIds;
                }
                if (vertexIds.Length != 2) return false;
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
            foreach (var entry in vertexIdEdgeIds)
            {
                if (entry.Value.Count != 2) return false;
            }
            //
            HashSet<int> usedEdgeIds = new HashSet<int>();
            foreach (var firstEdgeId in faceEdgeIds)
            {
                if (usedEdgeIds.Contains(firstEdgeId)) continue;
                List<int> edgeLoop = new List<int>();
                List<int> vertexLoop = new List<int>();
                int currentEdgeId = firstEdgeId;
                int currentVertexId = edgeIdVertexIds[currentEdgeId][0];
                do
                {
                    if (!usedEdgeIds.Add(currentEdgeId)) return false;
                    vertexLoop.Add(currentVertexId);
                    edgeLoop.Add(currentEdgeId);
                    int[] currentVertexIds = edgeIdVertexIds[currentEdgeId];
                    currentVertexId = currentVertexIds[0] == currentVertexId ? currentVertexIds[1] : currentVertexIds[0];
                    List<int> nextEdgeIds = vertexIdEdgeIds[currentVertexId];
                    currentEdgeId = nextEdgeIds[0] == currentEdgeId ? nextEdgeIds[1] : nextEdgeIds[0];
                }
                while (currentEdgeId != firstEdgeId);
                loopsEdgeIds.Add(edgeLoop.ToArray());
                loopsVertexIds.Add(vertexLoop.ToArray());
            }
            return true;
        }
        /// <summary>
        /// On which side of its first segment from the vertex an edge has the face: 1 on the left, where a cell of the
        /// face runs through the segment in its own direction, -1 on the right, and 0 when no cell of the face holds it.
        /// </summary>
        internal static int GetFaceSide(VisualizationData vis, int faceId, int edgeId, int startNodeId)
        {
            List<int> nodeIds = GetEdgeNodeIds(vis, edgeId, startNodeId);
            if (nodeIds == null || nodeIds.Count < 2) return 0;
            int a = nodeIds[0];
            int b = nodeIds[1];
            foreach (var cellId in vis.CellIdsByFace[faceId])
            {
                int[] cell = vis.Cells[cellId];
                int numOfCorners = cell.Length == 6 ? 3 : cell.Length == 8 ? 4 : cell.Length;
                for (int c = 0; c < numOfCorners; c++)
                {
                    int next = cell[(c + 1) % numOfCorners];
                    if (cell[c] == a && next == b) return 1;
                    if (cell[c] == b && next == a) return -1;
                }
            }
            return 0;
        }
        /// <summary>
        /// The frame of the plane of a closed polyline by Newell's method: its centre, its normal and two axes along
        /// it, turned so that the polyline runs counter-clockwise in them. Null when it has no area.
        /// </summary>
        internal static double[][] GetPlaneFrame(List<double[]> points)
        {
            double[] normal = new double[3];
            double[] centre = new double[3];
            int numOfPoints = points.Count;
            for (int i = 0; i < numOfPoints; i++)
            {
                double[] a = points[i];
                double[] b = points[(i + 1) % numOfPoints];
                normal[0] += (a[1] - b[1]) * (a[2] + b[2]);
                normal[1] += (a[2] - b[2]) * (a[0] + b[0]);
                normal[2] += (a[0] - b[0]) * (a[1] + b[1]);
                for (int d = 0; d < 3; d++) centre[d] += a[d];
            }
            double normalLength = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            if (normalLength == 0 || numOfPoints == 0) return null;
            for (int d = 0; d < 3; d++)
            {
                normal[d] /= normalLength;
                centre[d] /= numOfPoints;
            }
            double[] axis1 = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double along = axis1[0] * normal[0] + axis1[1] * normal[1] + axis1[2] * normal[2];
            for (int d = 0; d < 3; d++) axis1[d] -= along * normal[d];
            double axis1Length = Math.Sqrt(axis1[0] * axis1[0] + axis1[1] * axis1[1] + axis1[2] * axis1[2]);
            for (int d = 0; d < 3; d++) axis1[d] /= axis1Length;
            double[] axis2 = new double[] { normal[1] * axis1[2] - normal[2] * axis1[1],
                                            normal[2] * axis1[0] - normal[0] * axis1[2],
                                            normal[0] * axis1[1] - normal[1] * axis1[0] };
            return new double[][] { centre, normal, axis1, axis2 };
        }
        internal static double[] ToPlane(double[][] frame, double[] point)
        {
            double[] centre = frame[0];
            double[] axis1 = frame[2];
            double[] axis2 = frame[3];
            double[] r = new double[] { point[0] - centre[0], point[1] - centre[1], point[2] - centre[2] };
            return new double[] { r[0] * axis1[0] + r[1] * axis1[1] + r[2] * axis1[2],
                                  r[0] * axis2[0] + r[1] * axis2[1] + r[2] * axis2[2] };
        }
        /// <summary>
        /// The angle of the logical frame of a loop in the plane: the length-weighted circular mean, over its logical
        /// edges, of the angle of the chord of an edge less 90° times its direction. NaN when the chords have no length.
        /// </summary>
        private static double GetFrameAngle(List<double[]>[] polylines, int[] directions, int[] edgeTurns, double[][] frame)
        {
            double sumSin = 0;
            double sumCos = 0;
            for (int k = 0; k < polylines.Length; k++)
            {
                List<double[]> polyline = polylines[k];
                double[] start = ToPlane(frame, polyline[0]);
                double[] end = ToPlane(frame, polyline[polyline.Count - 1]);
                int turn = edgeTurns == null ? 0 : edgeTurns[k];
                if (turn == 0)
                {
                    AddChordAngle(start, end, directions[k], ref sumSin, ref sumCos);
                    continue;
                }
                List<List<double[]>> pieces = SplitPolyline(polyline, GetSplitFractions(turn));
                if (pieces == null) continue;
                for (int p = 0; p < pieces.Count; p++)
                {
                    double[] pieceStart = p == 0 ? start : ToPlane(frame, pieces[p][0]);
                    double[] pieceEnd = p == pieces.Count - 1 ? end : ToPlane(frame, pieces[p][pieces[p].Count - 1]);
                    AddChordAngle(pieceStart, pieceEnd, (directions[k] + p * Math.Sign(turn) + 4) % 4, ref sumSin,
                                  ref sumCos);
                }
            }
            if (sumSin == 0 && sumCos == 0) return double.NaN;
            return Math.Atan2(sumSin, sumCos);
        }
        private static void AddChordAngle(double[] start, double[] end, int direction, ref double sumSin, ref double sumCos)
        {
            double dx = end[0] - start[0];
            double dy = end[1] - start[1];
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;
            double angle = Math.Atan2(dy, dx) - 0.5 * Math.PI * direction;
            sumSin += length * Math.Sin(angle);
            sumCos += length * Math.Cos(angle);
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
        internal static double[] GetInteriorAnglesDeg(FeMesh mesh, VisualizationData vis, int faceId, int[] vertexIds)
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
        /// Adds the polylines of the logical edges of an edge with the given turn to the list, split at the fractions of
        /// GetSplitFractions, and the places of the logical edges that start at an inner corner of a convex turn edge,
        /// counted from firstEdge, to convexMiddleEdges. False when a turn edge has no length.
        /// </summary>
        private static bool AddLogicalPolylines(List<double[]> polyline, int turn, int firstEdge,
                                                List<List<double[]>> polylineList, List<int> convexMiddleEdges)
        {
            if (turn == 0)
            {
                polylineList.Add(polyline);
                return true;
            }
            List<List<double[]>> pieces = SplitPolyline(polyline, GetSplitFractions(turn));
            if (pieces == null) return false;
            for (int p = 0; p < pieces.Count; p++)
            {
                if (p > 0 && turn > 0) convexMiddleEdges.Add(firstEdge + polylineList.Count);
                polylineList.Add(pieces[p]);
            }
            return true;
        }
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of the grid of IsGridPoor, over the logical loop of the face,
        /// a turn edge split into its logical edges. The corners at the inner corners of a convex turn edge are left
        /// out. -1 when the grid folds or cannot be made, NaN when it is not judged.
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
                if (!AddLogicalPolylines(polyline, face.GetEdgeTurn(k), 0, polylineList, convexMiddleEdges)) return -1;
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
                                                 QuadSmoothing.GetScaledJacobian(points, quad, c, orientation));
                }
            }
            return minScaledJacobian;
        }
        /// <summary>
        /// The quality of GetGridQuality for a face with holes, in the plane of its outer loop given by its frame, over
        /// the polylines of the edges of every loop. All loops are snapped to one lattice step, set by the outer loop,
        /// which is refined for all of them while a loop does not close or the holes find no place; the holes are placed
        /// by SubmapHolePlacement on the plane coordinates of the nodes. A round hole is snapped as the square of four
        /// equal sides that the length around it gives, and its nodes are laid once its count is known
        /// (GetRoundLoopNodes). -1 when the grid folds or cannot be made.
        /// </summary>
        private static double GetGridQuality(FeMesh mesh, VisualizationData vis, SubmapFace face,
                                             List<double[]>[][] loopPolylines, double[][] frame)
        {
            SubmapFace[] loops = face.Loops;
            int numOfLoops = loops.Length;
            List<double[]>[][] logicalPolylines = new List<double[]>[numOfLoops][];
            double[][] lengths = new double[numOfLoops][];
            List<int> convexMiddleEdges = new List<int>();
            int firstEdge = 0;
            for (int l = 0; l < numOfLoops; l++)
            {
                SubmapFace loop = loops[l];
                List<List<double[]>> polylineList = new List<List<double[]>>();
                if (loop.IsRound)
                {
                    // The polylines of the edges of a round hole, which are split only once the counts are known
                    polylineList.AddRange(loopPolylines[l]);
                    lengths[l] = new double[4];
                    double total = 0;
                    foreach (var polyline in polylineList)
                    {
                        double edgeLength;
                        TransfiniteFaceDetector.Resample(polyline, 1, out edgeLength);
                        if (!(edgeLength > 0)) return -1;
                        total += edgeLength;
                    }
                    for (int k = 0; k < 4; k++) lengths[l][k] = 0.25 * total;
                    logicalPolylines[l] = polylineList.ToArray();
                    firstEdge += 4;
                    continue;
                }
                for (int k = 0; k < loop.EdgeIds.Length; k++)
                {
                    if (!AddLogicalPolylines(loopPolylines[l][k], loop.GetEdgeTurn(k), firstEdge, polylineList,
                                             convexMiddleEdges)) return -1;
                }
                logicalPolylines[l] = polylineList.ToArray();
                lengths[l] = new double[polylineList.Count];
                for (int k = 0; k < polylineList.Count; k++)
                {
                    TransfiniteFaceDetector.Resample(polylineList[k], 1, out lengths[l][k]);
                    if (!(lengths[l][k] > 0)) return -1;
                }
                firstEdge += polylineList.Count;
            }
            int[][] directions = face.LoopLogicalEdgeDirections;
            //
            int divisions = NumOfOutlineDivisions;
            for (int refinement = 0; refinement <= NumOfOutlineRefinements; refinement++, divisions *= 2)
            {
                int[][] counts = GetProportionalCounts(directions, lengths, divisions);
                if (counts == null) continue;
                double[][][][] edgeXyz = new double[numOfLoops][][][];
                double[][][][] edgeUv = new double[numOfLoops][][][];
                double[][][] nodePoints = new double[numOfLoops][][];
                double length;
                bool divisible = true;
                for (int l = 0; l < numOfLoops && divisible; l++)
                {
                    int n = counts[l].Length;
                    edgeXyz[l] = new double[n][][];
                    edgeUv[l] = new double[n][][];
                    if (loops[l].IsRound)
                    {
                        divisible = GetRoundLoopNodes(logicalPolylines[l], counts[l][0], frame, directions[0], counts[0],
                                                      nodePoints[0], edgeXyz[l], edgeUv[l], out nodePoints[l]);
                        continue;
                    }
                    List<double[]> points = new List<double[]>();
                    for (int k = 0; k < n; k++)
                    {
                        edgeXyz[l][k] = TransfiniteFaceDetector.Resample(logicalPolylines[l][k], counts[l][k], out length);
                        edgeUv[l][k] = new double[edgeXyz[l][k].Length][];
                        for (int i = 0; i < edgeXyz[l][k].Length; i++) edgeUv[l][k][i] = ToPlane(frame, edgeXyz[l][k][i]);
                        for (int i = 0; i < counts[l][k]; i++) points.Add(edgeUv[l][k][i]);
                    }
                    nodePoints[l] = points.ToArray();
                }
                if (!divisible) continue;
                int[][] holeOffsets;
                string reason;
                if (!SubmapHolePlacement.TryPlace(directions, counts, nodePoints, 1, out holeOffsets, out reason)) continue;
                //
                SubmapGrid grid;
                try
                {
                    grid = SubmapMapping.Map(directions, edgeUv, edgeXyz, holeOffsets, null);
                }
                catch (CaeException)
                {
                    return -1;
                }
                if (grid.NumberOfFoldedQuads > 0) return -1;
                if (convexMiddleEdges.Count == 0) return grid.MinScaledJacobian;
                // The grid again in the plane, without the corners at the middles of the convex turn edges
                int numOfBoundaryNodes = grid.NumberOfBoundaryNodes;
                double[][] gridPoints = new double[numOfBoundaryNodes + grid.NumberOfInnerNodes][];
                int edge = 0;
                for (int l = 0; l < numOfLoops; l++)
                {
                    for (int k = 0; k < counts[l].Length; k++, edge++)
                    {
                        for (int i = 0; i < counts[l][k]; i++) gridPoints[grid.GetBoundaryNodeId(edge, i)] = edgeUv[l][k][i];
                    }
                }
                for (int i = 0; i < grid.NumberOfInnerNodes; i++) gridPoints[numOfBoundaryNodes + i] = grid.InnerUv[i];
                HashSet<int> leftOutNodeIds = new HashSet<int>();
                foreach (var k in convexMiddleEdges) leftOutNodeIds.Add(grid.GetBoundaryNodeId(k, 0));
                double orientation = ThreeBlockMapping.GetSignedArea(gridPoints, grid.NumberOfOuterBoundaryNodes) < 0 ? -1 : 1;
                double minScaledJacobian = 1;
                foreach (var quad in grid.Quads)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        if (leftOutNodeIds.Contains(quad[c])) continue;
                        minScaledJacobian = Math.Min(minScaledJacobian,
                                                     QuadSmoothing.GetScaledJacobian(gridPoints, quad, c, orientation));
                    }
                }
                return minScaledJacobian;
            }
            return -1;
        }
        /// <summary>
        /// The nodes of a round hole for the grid of GetGridQuality, edgeCount elements on each of its four logical edges:
        /// spread evenly over the polylines of its edges, as many on each, in the plane of the frame, and started and
        /// turned by SubmapHolePlacement.GetRoundLoopOrder in the lattice the outer loop gives. edgeXyz and edgeUv take
        /// the four logical edges, and nodePoints the plane coordinates of the nodes in logical order. False when its
        /// edges cannot carry as many elements each.
        /// </summary>
        private static bool GetRoundLoopNodes(List<double[]>[] polylines, int edgeCount, double[][] frame,
                                              int[] outerDirections, int[] outerCounts, double[][] outerNodePoints,
                                              double[][][] edgeXyz, double[][][] edgeUv, out double[][] nodePoints)
        {
            nodePoints = null;
            int numOfNodes = 4 * edgeCount;
            if (numOfNodes % polylines.Length != 0) return false;
            int numOfEdgeElements = numOfNodes / polylines.Length;
            List<double[]> ringXyz = new List<double[]>();
            double length;
            foreach (var polyline in polylines)
            {
                double[][] samples = TransfiniteFaceDetector.Resample(polyline, numOfEdgeElements, out length);
                if (samples == null) return false;
                for (int i = 0; i < numOfEdgeElements; i++) ringXyz.Add(samples[i]);
            }
            double[][] ringUv = new double[numOfNodes][];
            for (int p = 0; p < numOfNodes; p++) ringUv[p] = ToPlane(frame, ringXyz[p]);
            int[] order = SubmapHolePlacement.GetRoundLoopOrder(outerDirections, outerCounts, outerNodePoints, ringUv);
            nodePoints = new double[numOfNodes][];
            for (int p = 0; p < numOfNodes; p++) nodePoints[p] = ringUv[order[p]];
            for (int c = 0; c < 4; c++)
            {
                edgeXyz[c] = new double[edgeCount + 1][];
                edgeUv[c] = new double[edgeCount + 1][];
                for (int i = 0; i <= edgeCount; i++)
                {
                    int node = order[(c * edgeCount + i) % numOfNodes];
                    edgeXyz[c][i] = ringXyz[node];
                    edgeUv[c][i] = ringUv[node];
                }
            }
            return true;
        }
        /// <summary>
        /// Where the polyline of an edge with the given turn is split into its logical edges, as fractions of its length:
        /// at its half for a single turn and at a quarter and three quarters for a double one.
        /// </summary>
        private static double[] GetSplitFractions(int turn)
        {
            return Math.Abs(turn) == 1 ? new double[] { 0.5 } : new double[] { 0.25, 0.75 };
        }
        /// <summary>
        /// The polyline split at the given fractions of its length, in increasing order, into pieces, the point at a
        /// fraction ending one piece and starting the next. Null when it has no length.
        /// </summary>
        private static List<List<double[]>> SplitPolyline(List<double[]> polyline, double[] fractions)
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
            if (!(total > 0)) return null;
            //
            List<List<double[]>> pieces = new List<List<double[]>>();
            List<double[]> piece = new List<double[]> { polyline[0] };
            int next = 1;
            int segment = 1;
            foreach (var fraction in fractions)
            {
                double target = fraction * total;
                while (segment < polyline.Count - 1 && lengths[segment] < target) segment++;
                double span = lengths[segment] - lengths[segment - 1];
                double f = span > 0 ? Math.Max(0, Math.Min(1, (target - lengths[segment - 1]) / span)) : 0;
                double[] p = polyline[segment - 1];
                double[] q = polyline[segment];
                double[] split = new double[] { p[0] + f * (q[0] - p[0]), p[1] + f * (q[1] - p[1]),
                                                p[2] + f * (q[2] - p[2]) };
                for (; next < segment; next++) piece.Add(polyline[next]);
                piece.Add(split);
                pieces.Add(piece);
                piece = new List<double[]> { split };
            }
            for (; next < polyline.Count; next++) piece.Add(polyline[next]);
            pieces.Add(piece);
            return pieces;
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
            int divisions = NumOfOutlineDivisions;
            for (int refinement = 0; refinement <= NumOfOutlineRefinements; refinement++)
            {
                int[][] counts = GetProportionalCounts(new int[][] { directions }, new double[][] { lengths }, divisions);
                if (counts != null) return counts[0];
                divisions *= 2;
            }
            return null;
        }
        /// <summary>
        /// The counts of GetProportionalCounts for every loop of a face, the outer loop first, with one lattice step:
        /// the perimeter of the ideal outline of the outer loop divided by the divisions. Every loop is snapped on its
        /// own, its first vertex at the origin. Null when an edge snaps to nothing or a loop does not close a valid
        /// outline on its own (SubmapMapping.AreCountsValid).
        /// </summary>
        private static int[][] GetProportionalCounts(int[][] loopDirections, double[][] loopLengths, int divisions)
        {
            int numOfLoops = loopDirections.Length;
            double[][][] outlines = new double[numOfLoops][][];
            for (int l = 0; l < numOfLoops; l++)
            {
                outlines[l] = GetIdealOutline(loopDirections[l], loopLengths[l]);
                if (outlines[l] == null) return null;
            }
            double perimeter = 0;
            int numOfOuterEdges = loopDirections[0].Length;
            for (int k = 0; k < numOfOuterEdges; k++)
            {
                double[] a = outlines[0][k];
                double[] b = outlines[0][(k + 1) % numOfOuterEdges];
                perimeter += Math.Abs(b[0] - a[0]) + Math.Abs(b[1] - a[1]);
            }
            double step = perimeter / divisions;
            //
            int[][] counts = new int[numOfLoops][];
            for (int l = 0; l < numOfLoops; l++)
            {
                int[] directions = loopDirections[l];
                double[][] outline = outlines[l];
                int n = directions.Length;
                long snappedX = 0;
                long snappedY = 0;
                counts[l] = new int[n];
                for (int k = 0; k < n; k++)
                {
                    // The last vertex is the first, at the origin
                    double[] next = outline[(k + 1) % n];
                    long nextX = (long)Math.Round(next[0] / step);
                    long nextY = (long)Math.Round(next[1] / step);
                    long count = DirectionI[directions[k]] * (nextX - snappedX) + DirectionJ[directions[k]] * (nextY - snappedY);
                    if (count < 1 || count > int.MaxValue) return null;
                    counts[l][k] = (int)count;
                    if (directions[k] % 2 == 0) nextY = snappedY;
                    else nextX = snappedX;
                    snappedX = nextX;
                    snappedY = nextY;
                }
            }
            string reason;
            if (!SubmapMapping.AreCountsValid(loopDirections, counts, null, out reason)) return null;
            return counts;
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
