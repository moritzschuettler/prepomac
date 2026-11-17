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
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// The grid of a face made of three-sided pieces, each three blocks of quads (ThreeBlockMapping), as GmshLensMesher
    /// and GmshThreeBlockMesher lay it on the nodes gmsh placed on the edges of the face. The nodes are those of the
    /// boundary followed by the nodes of the inner lines and of the pieces, each with its coordinates and its
    /// parameters on the surface; the boundary nodes carry their gmsh tags, the others get theirs when the grid is
    /// written. A piece with a pole of the surface at a corner is mapped in the pole chart of that pole
    /// (GmshPoleChart) and in the parameters of the surface, the best grid in space kept (MapPiece), the others in
    /// the parameters of the surface. An inner line runs straight in the parameters
    /// between its two nodes, an end at a pole taking the angular parameter of the other end (both ends at poles: the
    /// middle of the face), its nodes at equal lengths along it. Once all pieces are laid, the nodes inside the face
    /// are smoothed together (Smooth), so that the grid lines run on across the inner lines.
    /// </summary>
    internal class GmshLensGrid
    {
        // Variables                                                                                                                
        private int _surfaceId;
        private double _size;
        private double[] _periods;
        private double[] _min;
        private double[] _max;
        private List<IntPtr> _tags = new List<IntPtr>();
        private List<double[]> _xyz = new List<double[]>();
        private List<double[]> _uv = new List<double[]>();
        private Dictionary<int, GmshPoleChart> _poleCharts = new Dictionary<int, GmshPoleChart>();
        private List<int[]> _quads = new List<int[]>();
        private int _numOfFoldedQuads;
        private int _numOfBoundaryNodes;
        private const int NumOfLineSamples = 128;
        private const int NumOfSmoothingSweeps = 50;
        private const int SplitSearchWidth = 3;
        private const int MinPieceSideCount = 2;
        private const int MaxNumOfLineStarts = 4;
        private static readonly double[] CornerCutShares = new double[] { 0.25, 0.3, 0.35 };
        private const double FanLineAngleDeg = 90;
        private const int MinNumOfFanPieces = 3;


        // Properties                                                                                                               
        public int SurfaceId { get { return _surfaceId; } }
        public int NumberOfNodes { get { return _xyz.Count; } }
        public int NumberOfQuads { get { return _quads.Count; } }
        /// <summary>
        /// The quads that fold in the plane their piece was mapped in.
        /// </summary>
        public int NumberOfFoldedQuads { get { return _numOfFoldedQuads; } }
        public bool HasPoles { get { return _poleCharts.Count > 0; } }


        // Constructor                                                                                                              
        public GmshLensGrid(int surfaceId)
        {
            _surfaceId = surfaceId;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out _min, out _max);
            _periods = new double[] { GmshPoleChart.GetPeriod(surfaceId, 0, _min, _max),
                                      GmshPoleChart.GetPeriod(surfaceId, 1, _min, _max) };
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Adds a boundary node; the boundary nodes are added in loop order before any other node.
        /// </summary>
        public int AddBoundaryNode(IntPtr tag, double[] xyz)
        {
            _tags.Add(tag);
            _xyz.Add(xyz);
            _uv.Add(null);
            return _xyz.Count - 1;
        }
        /// <summary>
        /// Places the boundary nodes on the surface and finds which of the given nodes, the vertices of the face, are
        /// poles of it.
        /// </summary>
        public void SetBoundary(IEnumerable<int> vertexNodeIds)
        {
            _numOfBoundaryNodes = _xyz.Count;
            double[][] uv = GmshThreeBlockMesher.GetParametricCoordinates(_surfaceId, _xyz);
            for (int i = 0; i < uv.Length; i++) _uv[i] = uv[i];
            double[] low = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
            double[] high = new double[] { double.MinValue, double.MinValue, double.MinValue };
            foreach (var point in _xyz)
            {
                for (int c = 0; c < 3; c++)
                {
                    low[c] = Math.Min(low[c], point[c]);
                    high[c] = Math.Max(high[c], point[c]);
                }
            }
            _size = GmshPoleChart.GetDistance(low, high);
            //
            int p;
            double poleV;
            double farV;
            List<int> poleIds = new List<int>();
            Dictionary<int, double[]> poleBounds = new Dictionary<int, double[]>();
            foreach (var nodeId in vertexNodeIds.Distinct())
            {
                if (!GmshPoleChart.FindPole(_surfaceId, _xyz[nodeId], _size, out p, out poleV, out farV)) continue;
                poleIds.Add(nodeId);
                poleBounds.Add(nodeId, new double[] { p, poleV, farV });
            }
            if (poleIds.Count == 0) return;
            HashSet<int> poles = new HashSet<int>(poleIds);
            List<double[]> faceUv = new List<double[]>();
            for (int i = 0; i < _uv.Count; i++)
            {
                if (!poles.Contains(i)) faceUv.Add(_uv[i]);
            }
            foreach (var nodeId in poleIds)
            {
                double[] bounds = poleBounds[nodeId];
                _poleCharts.Add(nodeId, new GmshPoleChart(_surfaceId, _xyz[nodeId], (int)bounds[0], bounds[1],
                                                          bounds[2], faceUv));
            }
        }
        public bool IsPole(int nodeId)
        {
            return _poleCharts.ContainsKey(nodeId);
        }
        /// <summary>
        /// Lays the grid of a two-sided face whose side A runs from corner 0 to corner 1 and side B back, by the node
        /// ids of each: an inner line from a node of A to a node of B, at a pole inside a side, otherwise near the
        /// middle of each by length (ChooseSplit), and the two three-sided pieces it leaves.
        /// </summary>
        public bool LayTwoSided(int[] sideA, int[] sideB, out string reason)
        {
            int forcedA = GetPoleSplit(sideA);
            int forcedB = GetPoleSplit(sideB);
            int splitA;
            int splitB;
            int numOfLineElements;
            double score;
            if (!ChooseSplit(sideA, sideB, forcedA, forcedB, out splitA, out splitB, out numOfLineElements, out score))
            {
                reason = "no line between its sides carries a number of elements that both pieces it splits it " +
                         "into can take, with " + (sideA.Length - 1) + " and " + (sideB.Length - 1) + " on its sides";
                return false;
            }
            return LaySplit(sideA, sideB, splitA, splitB, numOfLineElements, out reason);
        }
        /// <summary>
        /// Lays the grid of a face with no corner, whose loop runs through the given node ids from its first node back
        /// to it, by a first line between two nodes of the loop (LayZeroSided). The line runs between two poles of the
        /// surface among the vertices, the given node ids; with one pole from it to the node opposite it by length;
        /// otherwise from each of the first MaxNumOfLineStarts vertices and from the node farthest from the centroid of
        /// the loop to the node opposite each, and the grid that is best in space once smoothed is laid (LayBest).
        /// </summary>
        public bool LayNoCorner(int[] loop, int[] vertexNodeIds, out string reason)
        {
            double[] fractions = GetLengthFractions(loop);
            List<int> poles = vertexNodeIds.Where(IsPole).Select(nodeId => Array.IndexOf(loop, nodeId)).ToList();
            List<int[]> lines = new List<int[]>();
            if (poles.Count >= 2) lines.Add(new int[] { poles[0], poles[1] });
            else
            {
                List<int> starts = new List<int>(poles);
                if (poles.Count == 0)
                {
                    starts.AddRange(vertexNodeIds.Take(MaxNumOfLineStarts)
                                                 .Select(nodeId => Array.IndexOf(loop, nodeId)));
                    starts.Add(GetFarthestNode(loop));
                }
                foreach (var start in starts)
                {
                    int end = GetOppositeNode(fractions, start);
                    if (end < 0 || lines.Any(line => line.Contains(start) && line.Contains(end))) continue;
                    lines.Add(new int[] { start, end });
                }
            }
            if (lines.Count == 0)
            {
                reason = "no line across it leaves " + LensFace.MinNumberOfElementsPerSide + " elements of its loop " +
                         "on either side, with " + (loop.Length - 1) + " on its loop";
                return false;
            }
            return LayBest(lines, line => LayLoopSplit(loop, line[0], line[1]), out reason);
        }
        /// <summary>
        /// Lays the grid of a face with one corner, whose loop runs through the given node ids from the corner back to
        /// it: a cut line from the node P at the share t of the length of the loop from the corner to the node Q at the
        /// same share from its other end leaves a three-sided piece at the corner - the loop from the corner to P, the
        /// line, the loop from Q back to the corner - and a two-sided rest, the loop from P to Q and the line back,
        /// laid as LayTwoSided lays one. Each of the shares CornerCutShares is tried (ChooseCornerCut), and the grid
        /// that is best in space once smoothed is laid (LayBest).
        /// </summary>
        public bool LayOneCorner(int[] loop, out string reason)
        {
            double[] fractions = GetLengthFractions(loop);
            List<int[]> cuts = new List<int[]>();
            foreach (var share in CornerCutShares)
            {
                int[] cut = ChooseCornerCut(loop, fractions, share);
                if (cut != null && !cuts.Any(other => other.SequenceEqual(cut))) cuts.Add(cut);
            }
            if (cuts.Count == 0)
            {
                reason = "no line across its corner carries a number of elements that the piece at the corner and " +
                         "the rest can take, with " + (loop.Length - 1) + " on its loop";
                return false;
            }
            return LayBest(cuts, cut => LayCornerCut(loop, cut), out reason);
        }
        /// <summary>
        /// Lays the grid of a face with a reentrant vertex at a pole of its surface, whose loop runs through the given
        /// node ids from its first node back to it: the angle at the pole is split by FanLineAngleDeg into k pieces,
        /// three at least, and lines of the surface parameters run from the pole to the boundary nodes nearest the
        /// angles that split it evenly in the chart of the pole (ChooseFanLines). Each piece - the pole, the ends of
        /// its lines and the corners of the face between them - is three- or four-sided and is mapped as one by
        /// MapPiece, so the pole carries one quad per piece.
        /// </summary>
        public bool LayPoleFan(int[] loop, int poleNodeId, HashSet<int> cornerNodeIds, out string reason)
        {
            reason = null;
            int m = loop.Length - 1;
            int start = Array.IndexOf(loop, poleNodeId);
            GmshPoleChart chart;
            if (start < 0 || start == m || !_poleCharts.TryGetValue(poleNodeId, out chart))
            {
                reason = "its reentrant vertex is no pole of its surface";
                return false;
            }
            int[] fan = Enumerable.Range(0, m + 1).Select(d => loop[(start + d) % m]).ToArray();
            double[] angles = fan.Select(nodeId => nodeId == poleNodeId ? double.NaN : chart.GetAngle(_uv[nodeId]))
                                 .ToArray();
            double first = angles[1];
            double poleAngle = angles[m - 1] - first;
            int numOfPieces = Math.Max(MinNumOfFanPieces, (int)Math.Round(Math.Abs(poleAngle) * 180 / Math.PI /
                                                                          FanLineAngleDeg,
                                                                          MidpointRounding.AwayFromZero));
            bool[] corners = fan.Select(nodeId => cornerNodeIds.Contains(nodeId)).ToArray();
            int[][] lines;
            if (!ChooseFanLines(fan, angles, corners, first, poleAngle, numOfPieces, out lines))
            {
                reason = "no lines from its pole into " + numOfPieces + " pieces of about " +
                         Math.Round(Math.Abs(poleAngle) * 180 / Math.PI / numOfPieces) + " degrees carry numbers of " +
                         "elements the pieces can take, with " + m + " on its loop";
                return false;
            }
            int[][] lineNodeIds = lines.Select(line => LayLine(poleNodeId, fan[line[0]], line[1])).ToArray();
            for (int j = 0; j < numOfPieces; j++)
            {
                List<int> piece = new List<int>();
                int from = j == 0 ? 0 : lines[j - 1][0];
                int to = j == numOfPieces - 1 ? m : lines[j][0];
                if (j > 0) piece.AddRange(lineNodeIds[j - 1].Take(lineNodeIds[j - 1].Length - 1));
                piece.AddRange(fan.Skip(from).Take(to - from));
                if (j < numOfPieces - 1) piece.AddRange(lineNodeIds[j].Reverse().Take(lineNodeIds[j].Length - 1));
                piece.Add(poleNodeId);
                HashSet<int> pieceCorners = new HashSet<int>(cornerNodeIds);
                pieceCorners.Add(poleNodeId);
                if (j > 0) pieceCorners.Add(fan[from]);
                if (j < numOfPieces - 1) pieceCorners.Add(fan[to]);
                int[][] sides = SplitAtCorners(piece, pieceCorners);
                if (sides.Length != 3 && sides.Length != 4)
                {
                    reason = "a piece between the lines from its pole has " + sides.Length + " corners";
                    return false;
                }
                if (!MapPiece(sides, out reason)) return false;
            }
            return true;
        }
        /// <summary>
        /// The lines of a face split at its pole, fan[0], into numOfPieces pieces: line i, of 1 to numOfPieces - 1,
        /// as the position in the loop of the node it ends at and its number of elements. Each end lies within
        /// SplitSearchWidth of the node whose angle round the pole is nearest the share i / numOfPieces of the angle at
        /// the pole, no corner of the face; each piece must be three-sided and valid for three blocks of quads, or
        /// four-sided with equal opposite sides, its sides of the loop of MinPieceSideCount elements at least. Of the
        /// valid choices the one is taken whose lines carry the numbers nearest their lengths over the mean of the
        /// boundary edges at their ends and end nearest their angles, by a dynamic program along the lines.
        /// </summary>
        private bool ChooseFanLines(int[] fan, double[] angles, bool[] corners, double first, double poleAngle,
                                    int numOfPieces, out int[][] lines)
        {
            lines = null;
            int m = fan.Length - 1;
            int poleNodeId = fan[0];
            double poleEdgeLength = (GetDistance(fan[0], fan[1]) + GetDistance(fan[m - 1], fan[m])) / 2;
            // The candidates of every line: the position of its end and its number of elements, with their cost
            List<int[]>[] candidates = new List<int[]>[numOfPieces + 1];
            List<double>[] costs = new List<double>[numOfPieces + 1];
            candidates[0] = new List<int[]> { new int[] { 0, 0 } };
            costs[0] = new List<double> { 0 };
            candidates[numOfPieces] = new List<int[]> { new int[] { m, 0 } };
            costs[numOfPieces] = new List<double> { 0 };
            for (int i = 1; i < numOfPieces; i++)
            {
                candidates[i] = new List<int[]>();
                costs[i] = new List<double>();
                double target = first + poleAngle * i / numOfPieces;
                int nearest = 1;
                for (int p = 2; p < m; p++)
                {
                    if (Math.Abs(angles[p] - target) < Math.Abs(angles[nearest] - target)) nearest = p;
                }
                int last = Math.Min(m - 1, nearest + SplitSearchWidth);
                for (int p = Math.Max(1, nearest - SplitSearchWidth); p <= last; p++)
                {
                    if (corners[p]) continue;
                    double meanLength = (GetDistance(fan[p - 1], fan[p]) + GetDistance(fan[p], fan[p + 1]) +
                                         2 * poleEdgeLength) / 4;
                    double length = Math.Max(1, GetLineLength(poleNodeId, fan[p]) / meanLength);
                    double angleCost = Math.Abs(angles[p] - target) / Math.Abs(poleAngle / numOfPieces);
                    for (int n = MinPieceSideCount; n <= 2 * (int)Math.Ceiling(length) + MinPieceSideCount; n++)
                    {
                        candidates[i].Add(new int[] { p, n });
                        costs[i].Add(Math.Abs(n - length) / length + angleCost);
                    }
                }
            }
            // The cheapest valid chain of candidates
            double[][] best = new double[numOfPieces + 1][];
            int[][] previous = new int[numOfPieces + 1][];
            for (int i = 0; i <= numOfPieces; i++)
            {
                best[i] = Enumerable.Repeat(double.MaxValue, candidates[i].Count).ToArray();
                previous[i] = Enumerable.Repeat(-1, candidates[i].Count).ToArray();
            }
            best[0][0] = 0;
            for (int i = 1; i <= numOfPieces; i++)
            {
                for (int c = 0; c < candidates[i].Count; c++)
                {
                    for (int b = 0; b < candidates[i - 1].Count; b++)
                    {
                        if (best[i - 1][b] == double.MaxValue) continue;
                        int[] from = candidates[i - 1][b];
                        int[] to = candidates[i][c];
                        if (from[0] >= to[0] || !IsFanPieceValid(corners, from, to, i == 1, i == numOfPieces))
                            continue;
                        double cost = best[i - 1][b] + costs[i][c];
                        if (cost < best[i][c])
                        {
                            best[i][c] = cost;
                            previous[i][c] = b;
                        }
                    }
                }
            }
            if (best[numOfPieces][0] == double.MaxValue) return false;
            lines = new int[numOfPieces - 1][];
            int index = previous[numOfPieces][0];
            for (int i = numOfPieces - 1; i >= 1; i--)
            {
                lines[i - 1] = candidates[i][index];
                index = previous[i][index];
            }
            return true;
        }
        /// <summary>
        /// Whether the piece of a face split at its pole between the line from[0] of from[1] elements, the loop from
        /// there to the end of the line to, and that line back to the pole is three-sided and valid for three blocks
        /// of quads, or four-sided with equal opposite sides; the first piece starts with the loop at the pole, the
        /// last ends with it.
        /// </summary>
        private static bool IsFanPieceValid(bool[] corners, int[] from, int[] to, bool firstPiece, bool lastPiece)
        {
            List<int> sideCounts = new List<int>();
            if (!firstPiece) sideCounts.Add(from[1]);
            int sideStart = from[0];
            for (int p = from[0] + 1; p <= to[0]; p++)
            {
                if (p < to[0] && !corners[p]) continue;
                sideCounts.Add(p - sideStart);
                sideStart = p;
            }
            if (!lastPiece) sideCounts.Add(to[1]);
            if (sideCounts.Any(count => count < MinPieceSideCount)) return false;
            if (sideCounts.Count == 3) return TransfiniteSideTriple.AreCountsValid(sideCounts.ToArray());
            if (sideCounts.Count == 4) return sideCounts[0] == sideCounts[2] && sideCounts[1] == sideCounts[3];
            return false;
        }
        // The sides of a closed loop of node ids, its last node its first, from the first corner on, each from one of
        // the given corners to the next
        private static int[][] SplitAtCorners(List<int> loop, HashSet<int> cornerNodeIds)
        {
            int m = loop.Count - 1;
            List<int> cornerPositions = Enumerable.Range(0, m).Where(p => cornerNodeIds.Contains(loop[p])).ToList();
            int[][] sides = new int[cornerPositions.Count][];
            for (int c = 0; c < cornerPositions.Count; c++)
            {
                int a = cornerPositions[c];
                int b = c + 1 < cornerPositions.Count ? cornerPositions[c + 1] : cornerPositions[0] + m;
                sides[c] = Enumerable.Range(a, b - a + 1).Select(p => loop[p % m]).ToArray();
            }
            return sides;
        }
        /// <summary>
        /// Lays each candidate by lay, which returns null when it laid it and the reason otherwise, smooths it (Smooth)
        /// and judges it in space (Judge), and takes the grid back off; then lays again the one with the fewest folded
        /// quads and, of as few, the best worst scaled Jacobian. The reason is that of the first candidate refused when
        /// none is laid.
        /// </summary>
        private bool LayBest(List<int[]> candidates, Func<int[], string> lay, out string reason)
        {
            if (candidates.Count == 1)
            {
                reason = lay(candidates[0]);
                return reason == null;
            }
            reason = null;
            int[] state = GetState();
            int best = -1;
            int bestNumOfFoldedQuads = 0;
            double bestMinScaledJacobian = 0;
            for (int c = 0; c < candidates.Count; c++)
            {
                string candidateReason = lay(candidates[c]);
                if (candidateReason == null)
                {
                    Smooth();
                    int numOfFoldedQuads;
                    double minScaledJacobian = Judge(out numOfFoldedQuads);
                    numOfFoldedQuads = Math.Max(numOfFoldedQuads, _numOfFoldedQuads);
                    if (best < 0 || numOfFoldedQuads < bestNumOfFoldedQuads ||
                        (numOfFoldedQuads == bestNumOfFoldedQuads && minScaledJacobian > bestMinScaledJacobian))
                    {
                        best = c;
                        bestNumOfFoldedQuads = numOfFoldedQuads;
                        bestMinScaledJacobian = minScaledJacobian;
                    }
                }
                else if (reason == null) reason = candidateReason;
                Restore(state);
            }
            if (best < 0) return false;
            reason = lay(candidates[best]);
            return reason == null;
        }
        // Lays the face of the loop split by a first line between the nodes at the positions start and end of the loop
        private string LayLoopSplit(int[] loop, int start, int end)
        {
            int m = loop.Length - 1;
            int[] sideA = Enumerable.Range(0, (end - start + m) % m + 1).Select(d => loop[(start + d) % m]).ToArray();
            int[] sideB = Enumerable.Range(0, (start - end + m) % m + 1).Select(d => loop[(end + d) % m]).ToArray();
            string reason;
            return LayZeroSided(sideA, sideB, out reason) ? null : reason;
        }
        // Lays the piece at the corner and the rest of a face with one corner, cut by the line from node cut[0] of the
        // loop to node cut[1] of cut[2] elements
        private string LayCornerCut(int[] loop, int[] cut)
        {
            int p = cut[0];
            int q = cut[1];
            int[] line = LayLine(loop[p], loop[q], cut[2]);
            string reason;
            int[][] piece = new int[][] { loop.Take(p + 1).ToArray(), line, loop.Skip(q).ToArray() };
            if (!MapPiece(piece, out reason)) return reason;
            int[] rest = loop.Skip(p).Take(q - p + 1).ToArray();
            if (!LayTwoSided(rest, line.Reverse().ToArray(), out reason)) return reason;
            return null;
        }
        /// <summary>
        /// The cut line across the corner of a face with one corner at the given share of the length of its loop from
        /// either end, as the positions p and q of its ends in the loop and its number of elements n: the nodes nearest
        /// the share moved by the fewest positions, within SplitSearchWidth, that leave a three-sided piece at the
        /// corner valid for three blocks of quads (TransfiniteSideTriple.AreCountsValid, its sides at the corner of
        /// MinPieceSideCount elements at least) and LensFace.MinNumberOfElementsPerSide elements at least on the line
        /// and on the loop from p to q; n the valid number nearest the length of the line over the mean of the four
        /// boundary edges at its ends, as ChooseSplit chooses it. Null when there is none.
        /// </summary>
        private int[] ChooseCornerCut(int[] loop, double[] fractions, double share)
        {
            int m = loop.Length - 1;
            int p0 = 1;
            int q0 = 1;
            for (int i = 1; i < m; i++)
            {
                if (Math.Abs(fractions[i] - share) < Math.Abs(fractions[p0] - share)) p0 = i;
                if (Math.Abs(fractions[i] - (1 - share)) < Math.Abs(fractions[q0] - (1 - share))) q0 = i;
            }
            int[] best = null;
            int bestChange = 0;
            double bestScore = 0;
            for (int p = p0 - SplitSearchWidth; p <= p0 + SplitSearchWidth; p++)
            {
                for (int q = q0 - SplitSearchWidth; q <= q0 + SplitSearchWidth; q++)
                {
                    if (p < MinPieceSideCount || m - q < MinPieceSideCount ||
                        q - p < LensFace.MinNumberOfElementsPerSide) continue;
                    int change = Math.Abs(p - p0) + Math.Abs(q - q0);
                    if (best != null && change > bestChange) continue;
                    double meanLength = (GetDistance(loop[p - 1], loop[p]) + GetDistance(loop[p], loop[p + 1]) +
                                         GetDistance(loop[q - 1], loop[q]) + GetDistance(loop[q], loop[q + 1])) / 4;
                    double target = Math.Max(1, GetLineLength(loop[p], loop[q]) / meanLength);
                    int n = -1;
                    for (int candidate = LensFace.MinNumberOfElementsPerSide; candidate < p + m - q; candidate++)
                    {
                        if (!TransfiniteSideTriple.AreCountsValid(new int[] { p, candidate, m - q })) continue;
                        if (n < 0 || Math.Abs(candidate - target) < Math.Abs(n - target)) n = candidate;
                    }
                    if (n < 0) continue;
                    double score = Math.Abs(n - target) / target;
                    if (best == null || change < bestChange || score < bestScore)
                    {
                        best = new int[] { p, q, n };
                        bestChange = change;
                        bestScore = score;
                    }
                }
            }
            return best;
        }
        // The position in the closed loop of the node nearest half the length of the loop from the node at the position
        // start that leaves LensFace.MinNumberOfElementsPerSide elements on either part of the loop; -1 for none
        private static int GetOppositeNode(double[] fractions, int start)
        {
            int m = fractions.Length - 1;
            int end = -1;
            double endDifference = double.MaxValue;
            for (int d = LensFace.MinNumberOfElementsPerSide; d <= m - LensFace.MinNumberOfElementsPerSide; d++)
            {
                int j = (start + d) % m;
                double share = fractions[j] - fractions[start];
                if (share < 0) share += 1;
                if (Math.Abs(share - 0.5) < endDifference)
                {
                    end = j;
                    endDifference = Math.Abs(share - 0.5);
                }
            }
            return end;
        }
        // The position in the closed loop of the node farthest in space from the centroid of the nodes of the loop
        private int GetFarthestNode(int[] loop)
        {
            int m = loop.Length - 1;
            double[] centroid = new double[3];
            for (int i = 0; i < m; i++)
            {
                for (int c = 0; c < 3; c++) centroid[c] += _xyz[loop[i]][c] / m;
            }
            int farthest = 0;
            for (int i = 1; i < m; i++)
            {
                if (GmshPoleChart.GetDistance(_xyz[loop[i]], centroid) >
                    GmshPoleChart.GetDistance(_xyz[loop[farthest]], centroid)) farthest = i;
            }
            return farthest;
        }
        // The numbers of nodes, of quads and of quads folded in their plane, to which Restore takes the grid back
        private int[] GetState()
        {
            return new int[] { _xyz.Count, _quads.Count, _numOfFoldedQuads };
        }
        private void Restore(int[] state)
        {
            RemoveNodesFrom(state[0]);
            _quads.RemoveRange(state[1], _quads.Count - state[1]);
            _numOfFoldedQuads = state[2];
        }
        /// <summary>
        /// Lays the grid of a face with no corner split by its first line into two parts of its loop, side A from the
        /// start of the line to its end and side B back: the line splits it into two two-sided faces, each split as
        /// LayTwoSided splits one, whose lines meet it at one node.
        /// </summary>
        private bool LayZeroSided(int[] sideA, int[] sideB, out string reason)
        {
            reason = null;
            int a = sideA.Length - 1;
            int first = sideA[0];
            int last = sideA[a];
            double meanLength = (GetDistance(sideA[0], sideA[1]) + GetDistance(sideA[a - 1], sideA[a]) +
                                 GetDistance(sideB[0], sideB[1]) +
                                 GetDistance(sideB[sideB.Length - 2], sideB[sideB.Length - 1])) / 4;
            double target = Math.Max(1, GetLineLength(first, last) / meanLength);
            int count = Math.Max(LensFace.MinNumberOfElementsPerSide, (int)Math.Round(target));
            if (count % 2 != a % 2)
            {
                bool up = count + 1 - target <= target - (count - 1) || count - 1 < LensFace.MinNumberOfElementsPerSide;
                count += up ? 1 : -1;
            }
            int[] counts = new int[] { count, count + 2, count - 2, count + 4 };
            int mark = _xyz.Count;
            foreach (var lineCount in counts)
            {
                if (lineCount < LensFace.MinNumberOfElementsPerSide) continue;
                int[] line = LayLine(first, last, lineCount);
                int[] reversed = line.Reverse().ToArray();
                // The node of the line both other lines end at
                double bestScore = double.MaxValue;
                int[] best = null;
                foreach (var j in GetCandidates(line, -1))
                {
                    int[] split1 = new int[3];
                    int[] split2 = new int[3];
                    double score1;
                    double score2;
                    if (!ChooseSplit(sideA, reversed, -1, lineCount - j, out split1[0], out split1[1], out split1[2],
                                     out score1) ||
                        !ChooseSplit(line, sideB, j, -1, out split2[0], out split2[1], out split2[2], out score2))
                        continue;
                    if (score1 + score2 < bestScore)
                    {
                        bestScore = score1 + score2;
                        best = split1.Concat(split2).ToArray();
                    }
                }
                if (best != null)
                {
                    return LaySplit(sideA, reversed, best[0], best[1], best[2], out reason) &&
                           LaySplit(line, sideB, best[3], best[4], best[5], out reason);
                }
                RemoveNodesFrom(mark);
            }
            reason = "no line across it and from its loop to that line carries a number of elements that the " +
                     "pieces can take, with " + a + " and " + (sideB.Length - 1) + " on the parts of its loop";
            return false;
        }
        /// <summary>
        /// Lays a three-sided piece by ThreeBlockMapping, or a four-sided one by FourSidedMapping; side k of it runs
        /// through the given node ids from corner k to corner k + 1. A piece with a pole at a corner is mapped in the
        /// chart of the pole and in the parameters of the surface, where the pole is the whole side of the piece at the
        /// bound of a parameter and is placed at the start, the middle and the end of it (GetPoleParameters), and the
        /// best grid in space is kept (IsBetter): the chart is far better at the apex of a cone, the parameters at
        /// times round the pole of a sphere.
        /// </summary>
        public bool MapPiece(int[][] sides, out string reason)
        {
            GmshPoleChart chart = null;
            int poleId = -1;
            for (int k = 0; k < sides.Length && chart == null; k++)
            {
                if (_poleCharts.TryGetValue(sides[k][0], out chart)) poleId = sides[k][0];
            }
            ThreeBlockGrid grid = null;
            double[][] innerUv = null;
            if (chart == null)
            {
                if (!TryMap(sides, null, -1, null, out grid, out innerUv, out reason)) return false;
            }
            else
            {
                reason = null;
                List<double[]> poleParameters = new List<double[]> { null };
                poleParameters.AddRange(GetPoleParameters(sides, chart));
                foreach (var poleUv in poleParameters)
                {
                    ThreeBlockGrid candidate;
                    double[][] candidateInnerUv;
                    string candidateReason;
                    if (!TryMap(sides, poleUv == null ? chart : null, poleId, poleUv, out candidate,
                                out candidateInnerUv, out candidateReason))
                    {
                        if (reason == null) reason = candidateReason;
                        continue;
                    }
                    if (grid == null || IsBetter(sides, candidate, candidateInnerUv, grid, innerUv))
                    {
                        grid = candidate;
                        innerUv = candidateInnerUv;
                    }
                }
                if (grid == null) return false;
                reason = null;
            }
            //
            _numOfFoldedQuads += grid.NumberOfFoldedQuads;
            int[] nodeIds = GetGridNodeIds(sides, grid);
            for (int i = 0; i < innerUv.Length; i++)
                nodeIds[grid.NumberOfBoundaryNodes + i] = AddNode(innerUv[i], grid.InnerXyz[i]);
            foreach (var quad in grid.Quads) _quads.Add(quad.Select(id => nodeIds[id]).ToArray());
            return true;
        }
        /// <summary>
        /// Maps a three-sided piece in the chart of the pole, or in the parameters of the surface without a chart, the
        /// pole then at poleUv, and gives the parameters of its inner nodes on the surface.
        /// </summary>
        private bool TryMap(int[][] sides, GmshPoleChart chart, int poleId, double[] poleUv, out ThreeBlockGrid grid,
                            out double[][] innerUv, out string reason)
        {
            grid = null;
            innerUv = null;
            reason = null;
            double[] reference = GetReference(sides);
            Func<int, double[]> getPoint = nodeId =>
            {
                if (chart != null) return nodeId == poleId ? new double[] { 0, 0 } : chart.ToChart(_uv[nodeId]);
                if (nodeId == poleId && poleUv != null) return poleUv;
                return Unwrap(_uv[nodeId], reference);
            };
            double[][][] sideUv = new double[sides.Length][][];
            double[][][] sideXyz = new double[sides.Length][][];
            for (int k = 0; k < sides.Length; k++)
            {
                sideUv[k] = sides[k].Select(getPoint).ToArray();
                sideXyz[k] = sides[k].Select(nodeId => _xyz[nodeId]).ToArray();
            }
            Func<double[], double[]> evaluate = points =>
            {
                if (chart != null) return chart.GetValues(points);
                double[] coor;
                Gmsh.Model.GetValue(2, _surfaceId, points, out coor);
                return coor;
            };
            try
            {
                if (sides.Length == 4) grid = FourSidedMapping.Map(sideUv, sideXyz, evaluate);
                else grid = ThreeBlockMapping.Map(sideUv, sideXyz, evaluate);
            }
            catch (CaeException ex)
            {
                reason = ex.Message.Trim().TrimEnd('.');
                if (reason.StartsWith("The ")) reason = "the " + reason.Substring(4);
                return false;
            }
            innerUv = grid.InnerUv.Select(point => chart != null ? chart.ToUv(point) : WrapIntoBounds(point)).ToArray();
            return true;
        }
        /// <summary>
        /// The parameters a pole of a piece is tried at in the parameters of the surface, which have no angular one for
        /// it: the start, the middle and the end of the angular range of the other boundary nodes of the piece, at the
        /// bound of the other parameter the pole lies at.
        /// </summary>
        private double[][] GetPoleParameters(int[][] sides, GmshPoleChart chart)
        {
            int p = chart.AngularDirection;
            double[] reference = GetReference(sides);
            double low = double.MaxValue;
            double high = double.MinValue;
            foreach (var side in sides)
            {
                foreach (var nodeId in side)
                {
                    if (IsPole(nodeId)) continue;
                    double u = Unwrap(_uv[nodeId], reference)[p];
                    low = Math.Min(low, u);
                    high = Math.Max(high, u);
                }
            }
            double[][] parameters = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                parameters[i] = new double[2];
                parameters[i][p] = low + (high - low) * i / 2;
                parameters[i][1 - p] = chart.PoleParameter;
            }
            return parameters;
        }
        // The parameters the periodic ones of a piece are unwrapped about: those of its first boundary node that is no
        // pole, whose angular parameter means nothing
        private double[] GetReference(int[][] sides)
        {
            foreach (var side in sides)
            {
                foreach (var nodeId in side)
                {
                    if (!IsPole(nodeId)) return _uv[nodeId];
                }
            }
            return _uv[sides[0][0]];
        }
        /// <summary>
        /// Whether the first grid of a piece is better than the second: fewer quads folded in its plane or in space, or
        /// as few and a better worst scaled Jacobian of a quad corner in space, at a pole against the normal of its
        /// quad.
        /// </summary>
        private bool IsBetter(int[][] sides, ThreeBlockGrid first, double[][] firstInnerUv, ThreeBlockGrid second,
                              double[][] secondInnerUv)
        {
            int firstFolded;
            int secondFolded;
            double firstMin = GetMinScaledJacobian(sides, first, firstInnerUv, out firstFolded);
            double secondMin = GetMinScaledJacobian(sides, second, secondInnerUv, out secondFolded);
            firstFolded = Math.Max(firstFolded, first.NumberOfFoldedQuads);
            secondFolded = Math.Max(secondFolded, second.NumberOfFoldedQuads);
            if (firstFolded != secondFolded) return firstFolded < secondFolded;
            return firstMin > secondMin;
        }
        private double GetMinScaledJacobian(int[][] sides, ThreeBlockGrid grid, double[][] innerUv,
                                            out int numOfFoldedQuads)
        {
            int[] nodeIds = GetGridNodeIds(sides, grid);
            Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, double[]> tagUv = new Dictionary<IntPtr, double[]>();
            List<IntPtr> poleTags = new List<IntPtr>();
            for (int i = 0; i < nodeIds.Length; i++)
            {
                IntPtr tag = new IntPtr(i + 1);
                bool inner = i >= grid.NumberOfBoundaryNodes;
                tagXyz[tag] = inner ? grid.InnerXyz[i - grid.NumberOfBoundaryNodes] : _xyz[nodeIds[i]];
                tagUv[tag] = inner ? innerUv[i - grid.NumberOfBoundaryNodes] : _uv[nodeIds[i]];
                if (!inner && IsPole(nodeIds[i])) poleTags.Add(tag);
            }
            IntPtr[] quadNodeTags = grid.Quads.SelectMany(quad => quad.Select(id => new IntPtr(id + 1))).ToArray();
            return GmshSeamGridMesher.GetMinScaledJacobian(_surfaceId, quadNodeTags, tagXyz, tagUv, poleTags,
                                                           out numOfFoldedQuads);
        }
        // The node ids of the face of the boundary nodes of the grid of a piece, by their grid ids, the inner ones -1
        private static int[] GetGridNodeIds(int[][] sides, ThreeBlockGrid grid)
        {
            int[] nodeIds = Enumerable.Repeat(-1, grid.NumberOfBoundaryNodes + grid.InnerUv.Length).ToArray();
            for (int k = 0; k < sides.Length; k++)
            {
                for (int i = 0; i < sides[k].Length - 1; i++) nodeIds[grid.GetBoundaryNodeId(k, i)] = sides[k][i];
            }
            return nodeIds;
        }
        /// <summary>
        /// Smooths every node that is not on the boundary of the face - the nodes of the inner lines and the inner
        /// nodes of the pieces - together by QuadSmoothing.SmoothRegion: the inner lines were laid first and stayed
        /// while each piece was smoothed on its own, so the grid lines bend where they cross them. The plane is the
        /// parameters of the surface in the frame fitted over the segments of the boundary (GmshQuadMesher.GetFrame)
        /// on a face without a pole, and the chart of the pole, the pole at its origin, on a face with one; a face with
        /// two poles or more, or whose grid folds, is left as it is. The smoothed grid is kept only when no quad folds
        /// in the plane or in space and its worst scaled Jacobian in space (Judge) is not lower than before; otherwise
        /// the nodes go back to where they were. Returns whether the smoothed grid was kept.
        /// </summary>
        public bool Smooth()
        {
            int numOfNodes = _xyz.Count;
            if (_poleCharts.Count > 1 || _numOfFoldedQuads > 0 || numOfNodes == _numOfBoundaryNodes) return false;
            // The points in the plane
            double[][] points = new double[numOfNodes][];
            GmshPoleChart chart = null;
            double[] frame = null;
            if (_poleCharts.Count == 1)
            {
                int poleId = _poleCharts.Keys.First();
                chart = _poleCharts[poleId];
                for (int i = 0; i < numOfNodes; i++)
                    points[i] = i == poleId ? new double[] { 0, 0 } : chart.ToChart(_uv[i]);
            }
            else
            {
                double[] reference = _uv[0];
                double[][] unwrapped = _uv.Select(uv => Unwrap(uv, reference)).ToArray();
                double[][][] segmentUv = new double[_numOfBoundaryNodes][][];
                double[][][] segmentXyz = new double[_numOfBoundaryNodes][][];
                for (int i = 0; i < _numOfBoundaryNodes; i++)
                {
                    int next = (i + 1) % _numOfBoundaryNodes;
                    segmentUv[i] = new double[][] { unwrapped[i], unwrapped[next] };
                    segmentXyz[i] = new double[][] { _xyz[i], _xyz[next] };
                }
                frame = GmshQuadMesher.GetFrame(segmentUv, segmentXyz);
                for (int i = 0; i < numOfNodes; i++) points[i] = ThreeBlockMapping.ToFrame(frame, unwrapped[i]);
            }
            int[][] quads = _quads.ToArray();
            double area = 0;
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    double[] p = points[quad[c]];
                    double[] q = points[quad[(c + 1) % 4]];
                    area += p[0] * q[1] - q[0] * p[1];
                }
            }
            double orientation = area < 0 ? -1 : 1;
            if (QuadSmoothing.CountFoldedElements(points, quads, orientation) > 0) return false;
            int numOfFoldedQuads;
            double minScaledJacobian = Judge(out numOfFoldedQuads);
            if (numOfFoldedQuads > 0) return false;
            // The smoothing, and the moved nodes on the surface
            int[] freeNodes = Enumerable.Range(_numOfBoundaryNodes, numOfNodes - _numOfBoundaryNodes).ToArray();
            QuadSmoothing.SmoothRegion(points, quads, freeNodes, orientation, NumOfSmoothingSweeps,
                                       SubmapFaceDetector.MinScaledJacobian);
            if (QuadSmoothing.CountFoldedElements(points, quads, orientation) > 0) return false;
            double[][] smoothedUv = new double[freeNodes.Length][];
            for (int i = 0; i < freeNodes.Length; i++)
            {
                double[] point = points[freeNodes[i]];
                smoothedUv[i] = chart != null ? chart.ToUv(point) :
                                                WrapIntoBounds(ThreeBlockMapping.FromFrame(frame, point));
            }
            double[] coor;
            Gmsh.Model.GetValue(2, _surfaceId, smoothedUv.SelectMany(uv => uv).ToArray(), out coor);
            if (coor == null || coor.Length != 3 * freeNodes.Length) return false;
            List<double[]> previousUv = _uv.GetRange(_numOfBoundaryNodes, freeNodes.Length);
            List<double[]> previousXyz = _xyz.GetRange(_numOfBoundaryNodes, freeNodes.Length);
            for (int i = 0; i < freeNodes.Length; i++)
            {
                _uv[freeNodes[i]] = smoothedUv[i];
                _xyz[freeNodes[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            }
            // Kept only when it is no worse in space
            double smoothedMinScaledJacobian = Judge(out numOfFoldedQuads);
            if (numOfFoldedQuads == 0 && smoothedMinScaledJacobian >= minScaledJacobian) return true;
            for (int i = 0; i < freeNodes.Length; i++)
            {
                _uv[freeNodes[i]] = previousUv[i];
                _xyz[freeNodes[i]] = previousXyz[i];
            }
            return false;
        }
        /// <summary>
        /// Writes the grid into the surface in place of any elements gmsh meshed on it, the quads turned with the
        /// normal of the surface or the way gmsh turned its elements; without quads every quad is split into two
        /// triangles.
        /// </summary>
        public void Write(bool quads)
        {
            List<IntPtr> newTags;
            List<double> newXyz;
            List<double> newUv;
            Dictionary<IntPtr, double[]> tagXyz;
            Dictionary<IntPtr, double[]> tagUv;
            IntPtr[] quadNodeTags = GetQuadNodeTags(out newTags, out newXyz, out newUv, out tagXyz, out tagUv);
            GmshSeamGridMesher.ReplaceSurfaceMesh(_surfaceId, quadNodeTags, newTags, newXyz, newUv, tagXyz, quads);
        }
        /// <summary>
        /// The length in space of the longest edge of a quad.
        /// </summary>
        public double GetLongestEdgeLength()
        {
            double longest = 0;
            foreach (var quad in _quads)
            {
                for (int c = 0; c < 4; c++)
                    longest = Math.Max(longest, GmshPoleChart.GetDistance(_xyz[quad[c]], _xyz[quad[(c + 1) % 4]]));
            }
            return longest;
        }
        /// <summary>
        /// The worst scaled Jacobian of a quad corner in space, at a pole against the normal of its own quad, and the
        /// number of quads with a corner that turns the other way.
        /// </summary>
        public double Judge(out int numOfFoldedQuads)
        {
            List<IntPtr> newTags;
            List<double> newXyz;
            List<double> newUv;
            Dictionary<IntPtr, double[]> tagXyz;
            Dictionary<IntPtr, double[]> tagUv;
            IntPtr[] quadNodeTags = GetQuadNodeTags(out newTags, out newXyz, out newUv, out tagXyz, out tagUv);
            HashSet<IntPtr> poleTags = new HashSet<IntPtr>(_poleCharts.Keys.Select(nodeId => _tags[nodeId]));
            return GmshSeamGridMesher.GetMinScaledJacobian(_surfaceId, quadNodeTags, tagXyz, tagUv, poleTags,
                                                           out numOfFoldedQuads);
        }
        /// <summary>
        /// The tags of the quads, four by four, the first quad one without a pole and starting at a node that is none,
        /// so that the surface has a normal where its turn is judged; the new nodes get the tags after the largest one
        /// gmsh has.
        /// </summary>
        private IntPtr[] GetQuadNodeTags(out List<IntPtr> newTags, out List<double> newXyz, out List<double> newUv,
                                         out Dictionary<IntPtr, double[]> tagXyz,
                                         out Dictionary<IntPtr, double[]> tagUv)
        {
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            IntPtr[] tags = new IntPtr[_xyz.Count];
            newTags = new List<IntPtr>();
            newXyz = new List<double>();
            newUv = new List<double>();
            tagXyz = new Dictionary<IntPtr, double[]>();
            tagUv = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < _xyz.Count; i++)
            {
                if (_tags[i] != IntPtr.Zero) tags[i] = _tags[i];
                else
                {
                    tags[i] = new IntPtr(++nodeTag);
                    newTags.Add(tags[i]);
                    newXyz.AddRange(_xyz[i]);
                    newUv.AddRange(_uv[i]);
                }
                tagXyz[tags[i]] = _xyz[i];
                tagUv[tags[i]] = _uv[i];
            }
            int first = 0;
            for (int q = 0; q < _quads.Count; q++)
            {
                if (_quads[q].Any(IsPole)) continue;
                first = q;
                break;
            }
            List<int[]> ordered = new List<int[]> { _quads[first] };
            for (int q = 0; q < _quads.Count; q++)
            {
                if (q != first) ordered.Add(_quads[q]);
            }
            IntPtr[] quadNodeTags = new IntPtr[4 * ordered.Count];
            for (int q = 0; q < ordered.Count; q++)
            {
                for (int c = 0; c < 4; c++) quadNodeTags[4 * q + c] = tags[ordered[q][c]];
            }
            return quadNodeTags;
        }
        /// <summary>
        /// The position of a pole inside a side, nearest its middle, at which the side must be split; -1 for none.
        /// </summary>
        private int GetPoleSplit(int[] side)
        {
            int split = -1;
            for (int i = 1; i < side.Length - 1; i++)
            {
                if (IsPole(side[i]) &&
                    (split < 0 || Math.Abs(2 * i - side.Length + 1) < Math.Abs(2 * split - side.Length + 1)))
                    split = i;
            }
            return split;
        }
        /// <summary>
        /// The split of a two-sided face by a line from node splitA of side A to node splitB of side B, and the number
        /// of elements of the line: the line from the pieces of A and of B of MinPieceSideCount elements at least that
        /// best keeps both pieces valid for three blocks of quads (TransfiniteSideTriple.AreCountsValid) with the
        /// number of elements its length asks for, at the nodes nearest the middle of each side by length, or at the
        /// forced ones. The score adds how far the number is off its length, how far each split is off the middle and
        /// how far the two are off facing each other, at the same share of the length of either side from the corner
        /// they share.
        /// </summary>
        private bool ChooseSplit(int[] sideA, int[] sideB, int forcedA, int forcedB, out int splitA, out int splitB,
                                 out int numOfLineElements, out double score)
        {
            splitA = -1;
            splitB = -1;
            numOfLineElements = 0;
            score = double.MaxValue;
            int a = sideA.Length - 1;
            int b = sideB.Length - 1;
            if ((a + b) % 2 != 0) return false;
            double[] fractionsA = GetLengthFractions(sideA);
            double[] fractionsB = GetLengthFractions(sideB);
            foreach (var iA in GetCandidates(sideA, forcedA))
            {
                foreach (var iB in GetCandidates(sideB, forcedB))
                {
                    int a1 = iA;
                    int a2 = a - iA;
                    int b1 = b - iB;
                    int b2 = iB;
                    if (Math.Min(Math.Min(a1, a2), Math.Min(b1, b2)) < MinPieceSideCount) continue;
                    int low = Math.Max(Math.Abs(a1 - b1), Math.Abs(a2 - b2)) + 1;
                    int high = Math.Min(a1 + b1, a2 + b2) - 1;
                    int parity = (a1 + b1) % 2;
                    double meanLength = (GetDistance(sideA[iA - 1], sideA[iA]) + GetDistance(sideA[iA], sideA[iA + 1]) +
                                         GetDistance(sideB[iB - 1], sideB[iB]) +
                                         GetDistance(sideB[iB], sideB[iB + 1])) / 4;
                    double target = Math.Max(1, GetLineLength(sideA[iA], sideB[iB]) / meanLength);
                    int n = -1;
                    for (int candidate = low; candidate <= high; candidate++)
                    {
                        if (candidate % 2 != parity) continue;
                        if (n < 0 || Math.Abs(candidate - target) < Math.Abs(n - target)) n = candidate;
                    }
                    if (n < 0) continue;
                    double candidateScore = Math.Abs(n - target) / target + Math.Abs(fractionsA[iA] - 0.5) +
                                            Math.Abs(fractionsB[iB] - 0.5) +
                                            Math.Abs(fractionsA[iA] + fractionsB[iB] - 1);
                    if (candidateScore < score)
                    {
                        score = candidateScore;
                        splitA = iA;
                        splitB = iB;
                        numOfLineElements = n;
                    }
                }
            }
            return splitA >= 0;
        }
        /// <summary>
        /// The positions a side may be split at: the forced one, or those within SplitSearchWidth of the node nearest
        /// its middle by length that leave MinPieceSideCount elements on either piece.
        /// </summary>
        private IEnumerable<int> GetCandidates(int[] side, int forced)
        {
            if (forced >= 0) return new int[] { forced };
            int n = side.Length - 1;
            double[] fractions = GetLengthFractions(side);
            int middle = MinPieceSideCount;
            for (int i = MinPieceSideCount; i <= n - MinPieceSideCount; i++)
            {
                if (Math.Abs(fractions[i] - 0.5) < Math.Abs(fractions[middle] - 0.5)) middle = i;
            }
            List<int> candidates = new List<int>();
            for (int i = middle - SplitSearchWidth; i <= middle + SplitSearchWidth; i++)
            {
                if (i >= MinPieceSideCount && i <= n - MinPieceSideCount) candidates.Add(i);
            }
            return candidates;
        }
        private bool LaySplit(int[] sideA, int[] sideB, int splitA, int splitB, int numOfLineElements,
                              out string reason)
        {
            int[] line = LayLine(sideA[splitA], sideB[splitB], numOfLineElements);
            int[][] first = new int[][] { sideA.Take(splitA + 1).ToArray(), line, sideB.Skip(splitB).ToArray() };
            int[][] second = new int[][] { sideA.Skip(splitA).ToArray(), sideB.Take(splitB + 1).ToArray(),
                                           line.Reverse().ToArray() };
            return MapPiece(first, out reason) && MapPiece(second, out reason);
        }
        /// <summary>
        /// The node ids of a line of the given number of elements from one node to the other, its inner nodes added.
        /// </summary>
        private int[] LayLine(int from, int to, int numOfElements)
        {
            double[][] samplesUv;
            double[][] samplesXyz;
            double[] lengths = GetLineSamples(from, to, out samplesUv, out samplesXyz);
            double total = lengths[NumOfLineSamples];
            List<double[]> nodeUv = new List<double[]>();
            int segment = 1;
            for (int j = 1; j < numOfElements; j++)
            {
                double target = total * j / numOfElements;
                while (segment < NumOfLineSamples && lengths[segment] < target) segment++;
                double span = lengths[segment] - lengths[segment - 1];
                double t = span > 0 ? Math.Max(0, Math.Min(1, (target - lengths[segment - 1]) / span)) : 0;
                double[] a = samplesUv[segment - 1];
                double[] b = samplesUv[segment];
                nodeUv.Add(new double[] { a[0] + t * (b[0] - a[0]), a[1] + t * (b[1] - a[1]) });
            }
            int[] line = new int[numOfElements + 1];
            line[0] = from;
            line[numOfElements] = to;
            if (nodeUv.Count > 0)
            {
                double[][] wrapped = nodeUv.Select(WrapIntoBounds).ToArray();
                double[] coor;
                Gmsh.Model.GetValue(2, _surfaceId, wrapped.SelectMany(uv => uv).ToArray(), out coor);
                for (int j = 0; j < wrapped.Length; j++)
                    line[j + 1] = AddNode(wrapped[j], new double[] { coor[3 * j], coor[3 * j + 1], coor[3 * j + 2] });
            }
            return line;
        }
        private double GetLineLength(int from, int to)
        {
            double[][] samplesUv;
            double[][] samplesXyz;
            return GetLineSamples(from, to, out samplesUv, out samplesXyz)[NumOfLineSamples];
        }
        /// <summary>
        /// The points of the line from one node to the other at equal steps of its parameters, and the lengths along
        /// it.
        /// </summary>
        private double[] GetLineSamples(int from, int to, out double[][] samplesUv, out double[][] samplesXyz)
        {
            double[] start = _uv[from].ToArray();
            double[] end = _uv[to].ToArray();
            GmshPoleChart fromChart;
            GmshPoleChart toChart;
            bool fromPole = _poleCharts.TryGetValue(from, out fromChart);
            bool toPole = _poleCharts.TryGetValue(to, out toChart);
            if (fromPole && toPole && fromChart.AngularDirection == toChart.AngularDirection)
            {
                int p = fromChart.AngularDirection;
                start[p] = fromChart.MiddleU;
                end[p] = fromChart.MiddleU;
                start[1 - p] = fromChart.PoleParameter;
                end[1 - p] = toChart.PoleParameter;
            }
            else if (fromPole)
            {
                int p = fromChart.AngularDirection;
                start[p] = end[p];
                start[1 - p] = fromChart.PoleParameter;
            }
            else if (toPole)
            {
                int p = toChart.AngularDirection;
                end[p] = start[p];
                end[1 - p] = toChart.PoleParameter;
            }
            start = WrapIntoBounds(start);
            end = Unwrap(end, start);
            samplesUv = new double[NumOfLineSamples + 1][];
            double[] uv = new double[2 * (NumOfLineSamples + 1)];
            for (int i = 0; i <= NumOfLineSamples; i++)
            {
                double t = (double)i / NumOfLineSamples;
                samplesUv[i] = new double[] { start[0] + t * (end[0] - start[0]), start[1] + t * (end[1] - start[1]) };
                uv[2 * i] = samplesUv[i][0];
                uv[2 * i + 1] = samplesUv[i][1];
            }
            double[] coor;
            Gmsh.Model.GetValue(2, _surfaceId, uv, out coor);
            samplesXyz = new double[NumOfLineSamples + 1][];
            double[] lengths = new double[NumOfLineSamples + 1];
            for (int i = 0; i <= NumOfLineSamples; i++)
            {
                samplesXyz[i] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
                if (i > 0) lengths[i] = lengths[i - 1] + GmshPoleChart.GetDistance(samplesXyz[i - 1], samplesXyz[i]);
            }
            return lengths;
        }
        // The share of the length of a side up to each of its nodes
        private double[] GetLengthFractions(int[] side)
        {
            double[] fractions = new double[side.Length];
            for (int i = 1; i < side.Length; i++) fractions[i] = fractions[i - 1] + GetDistance(side[i - 1], side[i]);
            double total = fractions[side.Length - 1];
            for (int i = 0; i < side.Length; i++) fractions[i] = total > 0 ? fractions[i] / total : 0;
            return fractions;
        }
        private int AddNode(double[] uv, double[] xyz)
        {
            _tags.Add(IntPtr.Zero);
            _xyz.Add(xyz);
            _uv.Add(uv);
            return _xyz.Count - 1;
        }
        private void RemoveNodesFrom(int nodeId)
        {
            _tags.RemoveRange(nodeId, _tags.Count - nodeId);
            _xyz.RemoveRange(nodeId, _xyz.Count - nodeId);
            _uv.RemoveRange(nodeId, _uv.Count - nodeId);
        }
        // The parameters within the bounds of the face, and each periodic one whose bounds span a whole period moved by
        // whole periods to the nearest of the reference
        private double[] Unwrap(double[] uv, double[] reference)
        {
            double[] unwrapped = WrapIntoBounds(uv);
            for (int d = 0; d < 2; d++)
            {
                double period = _periods[d];
                if (period > 0 && _max[d] - _min[d] >= period * (1 - 1E-6))
                    unwrapped[d] = GmshPoleChart.Wrap(uv[d], reference[d] - period / 2, period);
            }
            return unwrapped;
        }
        // The parameters with each periodic one outside the bounds of the surface wrapped to the nearer bound, the gap
        // between them split in its middle
        private double[] WrapIntoBounds(double[] uv)
        {
            double[] wrapped = uv.ToArray();
            for (int d = 0; d < 2; d++)
            {
                double period = _periods[d];
                double gap = period - (_max[d] - _min[d]);
                if (period > 0 && gap > 0 && (uv[d] < _min[d] || uv[d] > _max[d]))
                    wrapped[d] = GmshPoleChart.Wrap(uv[d], _min[d] - gap / 2, period);
            }
            return wrapped;
        }
        private double GetDistance(int nodeId1, int nodeId2)
        {
            return GmshPoleChart.GetDistance(_xyz[nodeId1], _xyz[nodeId2]);
        }
    }
}
