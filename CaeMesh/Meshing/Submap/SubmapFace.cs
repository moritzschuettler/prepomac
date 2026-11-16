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
    /// A face of a geometry part that is meshed by SubmapMapping: its one boundary loop as the vertices and the edges
    /// in the order the loop runs, every edge with the logical direction it runs in. Edge k runs from vertex k to
    /// vertex k + 1, and the directions turn left at an end, keep their way at a side and turn right at a corner, so
    /// that the logical loop is counter-clockwise.
    /// An edge may also turn in its middle, as a fillet does: such a turn edge is two logical edges, its first half in
    /// the direction of the edge and its second half turned by the turn of the edge, each with half its elements, so
    /// it needs an even number of them. An edge that turns by about 180°, a half circle, turns twice, at a quarter and
    /// at three quarters of its length: three logical edges of a quarter, a half and a quarter of its elements, so it
    /// needs a multiple of four. The logical loop, in which every turn edge is replaced by its pieces, is the one
    /// SubmapMapping maps; the edges are counted, balanced and named by their geometric ids. The sums over the sides
    /// of a loop list every logical edge as many times as its share of a common denominator: 1 without turn edges, 2
    /// with single turns only and 4 with a double turn, a whole edge then listed four times, a half twice and a quarter
    /// once, so that the sums stay integral.
    /// A face may have holes with corners: every hole is a SubmapFace of the same face id with its own vertices, edges,
    /// angles and edge turns and no holes of its own, traversed with the face on the same side as the outer loop, so
    /// that its logical loop is clockwise. Its directions are those of the logical frame of the outer loop. Every
    /// member of the logical view works on one loop; the members named All work on all loops, the outer one first.
    /// A hole may be round, a circle of one closed edge or of edges of equal length: its directions are not used, and
    /// in the lattice it is a square of a quarter of its elements on each side, its logical loop -j, -i, +j, +i from
    /// the corner SubmapHolePlacement finds once the counts are known. Its elements must be a multiple of four and
    /// evenly spread, so all its edges carry as many.
    /// </summary>
    [PmxType(2148)]
    public class SubmapFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int[] _vertexNodeIds;
        [PmxKey(2)] private int[] _edgeIds;
        [PmxKey(3)] private int[] _edgeDirections;
        [PmxKey(4)] private double[] _vertexAnglesDeg;
        [PmxKey(5)] private int[] _edgeTurns;
        [PmxKey(6)] private SubmapFace[] _holes;
        [PmxKey(7)] private bool _isRound;
        private const double SeparationTolerance = 1E-3;
        /// <summary>
        /// How much longer than the distance of a close hole corner along it an element at an end of the outer loop may
        /// be: the corner of the cell at the hole corner then keeps a scaled Jacobian of about 0.3.
        /// </summary>
        public const double CornerElementFactor = 1.7;
        private static readonly int[] DirectionX = new int[] { 1, 0, -1, 0 };
        private static readonly int[] DirectionY = new int[] { 0, 1, 0, -1 };


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the loop: vertex k is where edge k - 1 ends and edge k starts.
        /// </summary>
        public int[] VertexNodeIds { get { return _vertexNodeIds; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        /// <summary>
        /// The logical direction of every edge: 0, 1, 2 and 3 for +i, +j, -i and -j; of a turn edge that of its first
        /// half.
        /// </summary>
        public int[] EdgeDirections { get { return _edgeDirections; } }
        /// <summary>
        /// The interior angle of the face at every vertex.
        /// </summary>
        public double[] VertexAnglesDeg { get { return _vertexAnglesDeg; } }
        /// <summary>
        /// The turn every edge makes in its middle: 1 to the left, -1 to the right and 0 for none; 2 and -2 for an edge
        /// that turns twice; null for none at all.
        /// </summary>
        public int[] EdgeTurns { get { return _edgeTurns; } }
        public bool HasTurnEdges
        {
            get
            {
                if (_edgeTurns == null) return false;
                foreach (var turn in _edgeTurns)
                {
                    if (turn != 0) return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Whether the loop is a round hole, whose logical loop SubmapHolePlacement starts.
        /// </summary>
        public bool IsRound { get { return _isRound; } }
        /// <summary>
        /// The ids of the edges that turn in their middle, in loop order.
        /// </summary>
        public int[] TurnEdgeIds
        {
            get
            {
                List<int> turnEdgeIds = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    if (GetEdgeTurn(k) != 0) turnEdgeIds.Add(_edgeIds[k]);
                }
                return turnEdgeIds.ToArray();
            }
        }
        /// <summary>
        /// The direction of every logical edge, a turn edge giving one for each of its pieces; -j, -i, +j and +i for a
        /// round hole.
        /// </summary>
        public int[] LogicalEdgeDirections
        {
            get
            {
                if (_isRound) return new int[] { 3, 2, 1, 0 };
                List<int> directions = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    int step = Math.Sign(GetEdgeTurn(k));
                    for (int p = 0; p < GetNumberOfPieces(k); p++) directions.Add((_edgeDirections[k] + p * step + 4) % 4);
                }
                return directions.ToArray();
            }
        }
        /// <summary>
        /// The id of the edge every logical edge is, or is a piece of; the first edge for the four of a round hole.
        /// </summary>
        public int[] LogicalEdgeIds
        {
            get
            {
                if (_isRound) return new int[] { _edgeIds[0], _edgeIds[0], _edgeIds[0], _edgeIds[0] };
                List<int> edgeIds = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    for (int p = 0; p < GetNumberOfPieces(k); p++) edgeIds.Add(_edgeIds[k]);
                }
                return edgeIds.ToArray();
            }
        }
        /// <summary>
        /// The holes of the face, null for none.
        /// </summary>
        public SubmapFace[] Holes { get { return _holes; } }
        public bool HasHoles { get { return _holes != null && _holes.Length > 0; } }
        /// <summary>
        /// The loops of the face: the face itself as its outer loop, followed by its holes.
        /// </summary>
        public SubmapFace[] Loops
        {
            get
            {
                List<SubmapFace> loops = new List<SubmapFace> { this };
                if (_holes != null) loops.AddRange(_holes);
                return loops.ToArray();
            }
        }
        /// <summary>
        /// The ids of the edges of all loops.
        /// </summary>
        public int[] AllEdgeIds
        {
            get
            {
                List<int> edgeIds = new List<int>();
                foreach (var loop in Loops) edgeIds.AddRange(loop.EdgeIds);
                return edgeIds.ToArray();
            }
        }
        /// <summary>
        /// The ids of the logical edges of all loops, by which SubmapMapping names them.
        /// </summary>
        public int[] AllLogicalEdgeIds
        {
            get
            {
                List<int> edgeIds = new List<int>();
                foreach (var loop in Loops) edgeIds.AddRange(loop.LogicalEdgeIds);
                return edgeIds.ToArray();
            }
        }
        /// <summary>
        /// The ids of the edges of all loops that turn in their middle.
        /// </summary>
        public int[] AllTurnEdgeIds
        {
            get
            {
                List<int> edgeIds = new List<int>();
                foreach (var loop in Loops) edgeIds.AddRange(loop.TurnEdgeIds);
                return edgeIds.ToArray();
            }
        }
        /// <summary>
        /// The edges of all loops whose numbers of elements must be a multiple of more than one, by their ids, with the
        /// multiple (GetEdgeMultiple).
        /// </summary>
        public Dictionary<int, int> AllEdgeMultiples
        {
            get
            {
                Dictionary<int, int> edgeIdMultiple = new Dictionary<int, int>();
                foreach (var loop in Loops)
                {
                    for (int k = 0; k < loop._edgeIds.Length; k++)
                    {
                        int multiple = loop.GetEdgeMultiple(k);
                        int current;
                        if (multiple > 1 && !(edgeIdMultiple.TryGetValue(loop._edgeIds[k], out current) &&
                                              current >= multiple)) edgeIdMultiple[loop._edgeIds[k]] = multiple;
                    }
                }
                return edgeIdMultiple;
            }
        }
        /// <summary>
        /// The directions of the logical edges of every loop.
        /// </summary>
        public int[][] LoopLogicalEdgeDirections
        {
            get
            {
                SubmapFace[] loops = Loops;
                int[][] directions = new int[loops.Length][];
                for (int l = 0; l < loops.Length; l++) directions[l] = loops[l].LogicalEdgeDirections;
                return directions;
            }
        }


        // Constructors                                                                                                             
        public SubmapFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] edgeDirections, double[] vertexAnglesDeg,
                          int[] edgeTurns)
            : this(faceId, vertexNodeIds, edgeIds, edgeDirections, vertexAnglesDeg, edgeTurns, null)
        {
        }
        public SubmapFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] edgeDirections, double[] vertexAnglesDeg,
                          int[] edgeTurns, SubmapFace[] holes)
            : this(faceId, vertexNodeIds, edgeIds, edgeDirections, vertexAnglesDeg, edgeTurns, holes, false)
        {
        }
        public SubmapFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] edgeDirections, double[] vertexAnglesDeg,
                          int[] edgeTurns, SubmapFace[] holes, bool isRound)
        {
            _faceId = faceId;
            _vertexNodeIds = vertexNodeIds;
            _edgeIds = edgeIds;
            _edgeDirections = edgeDirections;
            _vertexAnglesDeg = vertexAnglesDeg;
            _edgeTurns = edgeTurns;
            _holes = holes != null && holes.Length > 0 ? holes : null;
            _isRound = isRound;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The turn edge k makes in its middle.
        /// </summary>
        public int GetEdgeTurn(int k)
        {
            return _edgeTurns == null ? 0 : _edgeTurns[k];
        }
        /// <summary>
        /// The number of logical edges edge k is: one, two for an edge that turns once and three for one that turns
        /// twice.
        /// </summary>
        private int GetNumberOfPieces(int k)
        {
            return 1 + Math.Abs(GetEdgeTurn(k));
        }
        /// <summary>
        /// The number the elements of edge k must be a multiple of: 2 for an edge that turns once, 4 for one that turns
        /// twice, and for an edge of a round hole of m edges 4 over the greatest common divisor of 4 and m, so that
        /// the equal counts of its edges add up to a multiple of four; otherwise 1.
        /// </summary>
        public int GetEdgeMultiple(int k)
        {
            if (_isRound)
            {
                int m = _edgeIds.Length;
                return m % 4 == 0 ? 1 : m % 2 == 0 ? 2 : 4;
            }
            int turn = Math.Abs(GetEdgeTurn(k));
            return turn == 0 ? 1 : turn == 1 ? 2 : 4;
        }
        /// <summary>
        /// The common denominator of the sums of the loop: 4 for a round hole or a loop with an edge that turns twice,
        /// 2 for one with edges that turn once, and 1 for one without turn edges.
        /// </summary>
        public int GetDenominator()
        {
            if (_isRound) return 4;
            int denominator = 1;
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                int turn = Math.Abs(GetEdgeTurn(k));
                if (turn == 2) return 4;
                if (turn == 1) denominator = 2;
            }
            return denominator;
        }
        /// <summary>
        /// The common denominator of the sums over all loops: the largest of GetDenominator.
        /// </summary>
        private int GetAllDenominator()
        {
            int denominator = 1;
            foreach (var loop in Loops) denominator = Math.Max(denominator, loop.GetDenominator());
            return denominator;
        }
        /// <summary>
        /// The two pairs of sides whose numbers of elements must be equal for the logical outline to close: the edges
        /// along +i against those along -i, and the edges along +j against those along -j. Every logical edge is listed
        /// in its side as many times as its share of the denominator of the loop (GetDenominator): without turn edges
        /// once, with single turns every edge that does not turn twice and every half once, and with a double turn
        /// every whole edge four times, a half twice and a quarter once. A round hole, which closes whatever its count,
        /// gives instead a pair for every two edges that follow each other, which carry as many elements.
        /// </summary>
        public TransfiniteSidePair[] GetSidePairs()
        {
            if (_isRound)
            {
                TransfiniteSidePair[] roundPairs = new TransfiniteSidePair[_edgeIds.Length - 1];
                for (int k = 0; k < roundPairs.Length; k++)
                    roundPairs[k] = new TransfiniteSidePair(_faceId, new int[] { _edgeIds[k] }, new int[] { _edgeIds[k + 1] });
                return roundPairs;
            }
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int[] multiplicities = GetLogicalMultiplicities(GetDenominator());
            TransfiniteSidePair[] pairs = new TransfiniteSidePair[2];
            for (int axis = 0; axis < 2; axis++)
            {
                List<int> forward = new List<int>();
                List<int> backward = new List<int>();
                for (int k = 0; k < edgeIds.Length; k++)
                {
                    for (int m = 0; m < multiplicities[k]; m++)
                    {
                        if (directions[k] == axis) forward.Add(edgeIds[k]);
                        else if (directions[k] == axis + 2) backward.Add(edgeIds[k]);
                    }
                }
                pairs[axis] = new TransfiniteSidePair(_faceId, forward.ToArray(), backward.ToArray());
            }
            return pairs;
        }
        /// <summary>
        /// The pairs of GetSidePairs of every loop, each loop closing on its own: those of the outer loop first.
        /// </summary>
        public List<TransfiniteSidePair> GetAllSidePairs()
        {
            List<TransfiniteSidePair> pairs = new List<TransfiniteSidePair>();
            foreach (var loop in Loops) pairs.AddRange(loop.GetSidePairs());
            return pairs;
        }
        /// <summary>
        /// The pairs of parallel sides that face each other across the face and must keep at least the bound of lattice
        /// cells between them, a side being a run of logical edges in one direction. Two sides are such a pair when, in
        /// the ideal outline the edge lengths draw (SubmapFaceDetector.GetIdealOutline), they lie apart and their
        /// extents along them overlap, both beyond a tolerance relative to the size of the outline. edgeLengths holds
        /// the length of every edge in loop order, a half having half the length of its edge. The pairs do not prove
        /// the lattice outline simple; they keep the order of the sides the face has. With turn edges the sums and the
        /// bound are multiplied by the denominator of the loop as in GetSidePairs.
        /// </summary>
        public List<SubmapSeparation> GetSeparations(double[] edgeLengths, int bound)
        {
            List<SubmapSeparation> separations = new List<SubmapSeparation>();
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int denominator = GetDenominator();
            int[] multiplicities = GetLogicalMultiplicities(denominator);
            double[] lengths = GetLogicalEdgeLengths(edgeLengths);
            bound *= denominator;
            double[][] outline = SubmapFaceDetector.GetIdealOutline(directions, lengths);
            if (outline == null) return separations;
            //
            int n = edgeIds.Length;
            List<int> sideStarts = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (directions[k] != directions[(k + n - 1) % n]) sideStarts.Add(k);
            }
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (var point in outline)
            {
                minX = Math.Min(minX, point[0]);
                maxX = Math.Max(maxX, point[0]);
                minY = Math.Min(minY, point[1]);
                maxY = Math.Max(maxY, point[1]);
            }
            double tolerance = SeparationTolerance * Math.Max(maxX - minX, maxY - minY);
            // Every side by its axis, its coordinate across and its extent along
            int numOfSides = sideStarts.Count;
            int[] axes = new int[numOfSides];
            double[] across = new double[numOfSides];
            double[] alongMin = new double[numOfSides];
            double[] alongMax = new double[numOfSides];
            for (int s = 0; s < numOfSides; s++)
            {
                int start = sideStarts[s];
                int end = sideStarts[(s + 1) % numOfSides];
                axes[s] = directions[start] % 2;
                across[s] = outline[start][1 - axes[s]];
                alongMin[s] = Math.Min(outline[start][axes[s]], outline[end][axes[s]]);
                alongMax[s] = Math.Max(outline[start][axes[s]], outline[end][axes[s]]);
            }
            //
            for (int a = 0; a < numOfSides; a++)
            {
                for (int b = a + 1; b < numOfSides; b++)
                {
                    if (axes[a] != axes[b] || Math.Abs(across[b] - across[a]) <= tolerance) continue;
                    if (Math.Min(alongMax[a], alongMax[b]) - Math.Max(alongMin[a], alongMin[b]) <= tolerance) continue;
                    //
                    int first = across[a] < across[b] ? a : b;
                    int second = first == a ? b : a;
                    // The lattice coordinate across grows along +j for sides along i and along +i for sides along j
                    int towards = axes[a] == 0 ? 1 : 0;
                    List<int> towardsEdgeIds = new List<int>();
                    List<int> backEdgeIds = new List<int>();
                    for (int k = sideStarts[first]; k != sideStarts[second]; k = (k + 1) % n)
                    {
                        for (int m = 0; m < multiplicities[k]; m++)
                        {
                            if (directions[k] == towards) towardsEdgeIds.Add(edgeIds[k]);
                            else if (directions[k] == towards + 2) backEdgeIds.Add(edgeIds[k]);
                        }
                    }
                    separations.Add(new SubmapSeparation(_faceId, towardsEdgeIds.ToArray(), backEdgeIds.ToArray(), bound,
                                                         edgeIds[sideStarts[first]], edgeIds[sideStarts[second]]));
                }
            }
            return separations;
        }
        /// <summary>
        /// The numbers of elements of the edges in loop order, read from the counts of the edges by their ids.
        /// </summary>
        public int[] GetEdgeCounts(Dictionary<int, int> edgeIdCount)
        {
            int[] counts = new int[_edgeIds.Length];
            for (int k = 0; k < _edgeIds.Length; k++) counts[k] = edgeIdCount[_edgeIds[k]];
            return counts;
        }
        /// <summary>
        /// The numbers of elements of the logical edges, a turn edge giving half its count to each half, the larger
        /// half second when the count is odd, and an edge that turns twice a quarter to its first and its last piece
        /// and the rest to its middle one. A round hole gives a quarter of the elements around it to each side.
        /// </summary>
        public int[] GetLogicalEdgeCounts(Dictionary<int, int> edgeIdCount)
        {
            List<int> counts = new List<int>();
            if (_isRound)
            {
                int total = 0;
                foreach (var edgeId in _edgeIds) total += edgeIdCount[edgeId];
                for (int p = 0; p < 4; p++) counts.Add(total / 4);
                return counts.ToArray();
            }
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                int count = edgeIdCount[_edgeIds[k]];
                int turn = Math.Abs(GetEdgeTurn(k));
                if (turn == 0) counts.Add(count);
                else if (turn == 1)
                {
                    counts.Add(count / 2);
                    counts.Add(count - count / 2);
                }
                else
                {
                    counts.Add(count / 4);
                    counts.Add(count - 2 * (count / 4));
                    counts.Add(count / 4);
                }
            }
            return counts.ToArray();
        }
        /// <summary>
        /// The numbers of elements of the logical edges of every loop, as GetLogicalEdgeCounts gives them.
        /// </summary>
        public int[][] GetLoopLogicalEdgeCounts(Dictionary<int, int> edgeIdCount)
        {
            SubmapFace[] loops = Loops;
            int[][] counts = new int[loops.Length][];
            for (int l = 0; l < loops.Length; l++) counts[l] = loops[l].GetLogicalEdgeCounts(edgeIdCount);
            return counts;
        }
        /// <summary>
        /// Whether the counts close every loop into a simple lattice outline on its own, the outer one counter-clockwise
        /// and every hole clockwise (SubmapMapping.AreCountsValid), the reason naming the logical edges of all loops by
        /// their places.
        /// </summary>
        public bool AreCountsValid(Dictionary<int, int> edgeIdCount, out string reason)
        {
            return SubmapMapping.AreCountsValid(LoopLogicalEdgeDirections, GetLoopLogicalEdgeCounts(edgeIdCount), null,
                                                out reason);
        }
        /// <summary>
        /// Why the counts cannot be split into the logical edges of all loops, naming the first edge that cannot: a turn
        /// edge with an odd number of elements, an edge that turns twice with a number that is not a multiple of four,
        /// a round hole whose edges carry unequal numbers or a number around it that is not a multiple of four. Null when
        /// all can be split.
        /// </summary>
        public string GetIndivisibleCountReason(Dictionary<int, int> edgeIdCount)
        {
            foreach (var loop in Loops)
            {
                if (loop._isRound)
                {
                    int first = edgeIdCount[loop._edgeIds[0]];
                    int total = 0;
                    for (int k = 0; k < loop._edgeIds.Length; k++)
                    {
                        int count = edgeIdCount[loop._edgeIds[k]];
                        if (count != first)
                            return "Edge " + loop._edgeIds[0] + " and Edge " + loop._edgeIds[k] + " of a round hole carry " +
                                   first + " and " + count + " elements, which must be equal";
                        total += count;
                    }
                    if (total % 4 != 0)
                        return "the round hole of Edge " + loop._edgeIds[0] + " carries " + total + " elements around it, " +
                               "not a multiple of four";
                    continue;
                }
                for (int k = 0; k < loop._edgeIds.Length; k++)
                {
                    int count = edgeIdCount[loop._edgeIds[k]];
                    int turn = Math.Abs(loop.GetEdgeTurn(k));
                    if (turn == 1 && count % 2 != 0)
                        return "Edge " + loop._edgeIds[k] + " turns a corner in its middle and carries an odd number of " +
                               "elements";
                    if (turn == 2 && count % 4 != 0)
                        return "Edge " + loop._edgeIds[k] + " turns twice and carries " + count + " elements, not a " +
                               "multiple of four";
                }
            }
            return null;
        }
        /// <summary>
        /// The 2D points of the logical vertices of every loop in the logical frame of the outer loop: logical +i along
        /// x and +j along y. loopVertexPoints holds the 2D coordinates of the vertices of every loop in any frame of
        /// the plane in which the outer loop runs counter-clockwise; the frame is turned by the length-weighted circular
        /// mean of the angle of every outer edge, its chord against its direction, a turn edge counting half its turn.
        /// The middle of a turn edge is the corner its two halves would meet at; an edge that turns twice has its two
        /// corners out from its ends by half its chord, as a half circle bulges by its radius. A round hole is given by
        /// points on its circle, as many as wanted, whose mean is its centre and whose mean distance from it its radius:
        /// its logical points are the four corners of its square in the lattice, on the circle at 45° to the frame,
        /// the +i +j one first and clockwise, followed by the four points where the circle reaches furthest along +i,
        /// -j, -i and +j, so that the points span the circle. Null when the outer loop gives no frame.
        /// </summary>
        public double[][][] GetLogicalLoopPoints(double[][][] loopVertexPoints)
        {
            SubmapFace[] loops = Loops;
            if (loopVertexPoints == null || loopVertexPoints.Length != loops.Length) return null;
            double sumSin = 0;
            double sumCos = 0;
            int n = _edgeIds.Length;
            for (int k = 0; k < n; k++)
            {
                double[] a = loopVertexPoints[0][k];
                double[] b = loopVertexPoints[0][(k + 1) % n];
                double dx = b[0] - a[0];
                double dy = b[1] - a[1];
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length == 0) continue;
                double angle = Math.Atan2(dy, dx) - 0.5 * Math.PI * (_edgeDirections[k] + 0.5 * GetEdgeTurn(k));
                sumSin += length * Math.Sin(angle);
                sumCos += length * Math.Cos(angle);
            }
            if (sumSin == 0 && sumCos == 0) return null;
            double frameAngle = Math.Atan2(sumSin, sumCos);
            double cos = Math.Cos(frameAngle);
            double sin = Math.Sin(frameAngle);
            //
            double[][][] logicalPoints = new double[loops.Length][][];
            for (int l = 0; l < loops.Length; l++)
            {
                SubmapFace loop = loops[l];
                int numOfVertices = loop._isRound ? (loopVertexPoints[l] == null ? 0 : loopVertexPoints[l].Length) :
                                                    loop._edgeIds.Length;
                if (loopVertexPoints[l] == null || loopVertexPoints[l].Length != numOfVertices || numOfVertices == 0)
                    return null;
                double[][] vertices = new double[numOfVertices][];
                for (int k = 0; k < numOfVertices; k++)
                {
                    double[] p = loopVertexPoints[l][k];
                    vertices[k] = new double[] { cos * p[0] + sin * p[1], -sin * p[0] + cos * p[1] };
                }
                if (loop._isRound)
                {
                    logicalPoints[l] = GetRoundLoopPoints(vertices);
                    continue;
                }
                List<double[]> points = new List<double[]>();
                for (int k = 0; k < numOfVertices; k++)
                {
                    double[] a = vertices[k];
                    double[] b = vertices[(k + 1) % numOfVertices];
                    points.Add(a);
                    int turn = loop.GetEdgeTurn(k);
                    if (turn == 0) continue;
                    if (Math.Abs(turn) == 1)
                    {
                        if (loop._edgeDirections[k] % 2 == 0) points.Add(new double[] { b[0], a[1] });
                        else points.Add(new double[] { a[0], b[1] });
                        continue;
                    }
                    int direction = loop._edgeDirections[k];
                    double depth = 0.5 * Distance(a, b);
                    points.Add(new double[] { a[0] + depth * DirectionX[direction], a[1] + depth * DirectionY[direction] });
                    points.Add(new double[] { b[0] + depth * DirectionX[direction], b[1] + depth * DirectionY[direction] });
                }
                logicalPoints[l] = points.ToArray();
            }
            return logicalPoints;
        }
        /// <summary>
        /// The logical points of a round hole of GetLogicalLoopPoints from points on its circle in the logical frame.
        /// </summary>
        private static double[][] GetRoundLoopPoints(double[][] circlePoints)
        {
            double[] centre = new double[2];
            foreach (var point in circlePoints)
            {
                centre[0] += point[0] / circlePoints.Length;
                centre[1] += point[1] / circlePoints.Length;
            }
            double radius = 0;
            foreach (var point in circlePoints) radius += Distance(centre, point) / circlePoints.Length;
            double diagonal = radius * Math.Sqrt(0.5);
            return new double[][]
            {
                new double[] { centre[0] + diagonal, centre[1] + diagonal },
                new double[] { centre[0] + diagonal, centre[1] - diagonal },
                new double[] { centre[0] - diagonal, centre[1] - diagonal },
                new double[] { centre[0] - diagonal, centre[1] + diagonal },
                new double[] { centre[0] + radius, centre[1] }, new double[] { centre[0], centre[1] - radius },
                new double[] { centre[0] - radius, centre[1] }, new double[] { centre[0], centre[1] + radius }
            };
        }
        /// <summary>
        /// The rows that leave room in the lattice for every hole and a bound of cells on each side of it: for every
        /// pair of parallel sides of the outer loop that lie on opposite sides of a hole and whose extents along them
        /// overlap that of the hole, the path along the outer loop from the first side to the second, which is how far
        /// apart they lie in the lattice, less the width of the hole, the sum of its logical edges running across,
        /// must be at least twice the bound; with two holes between them that follow each other across, less both
        /// widths, at least three times. A side is a run of logical edges in one direction; where the sides and the
        /// holes lie is read off loopVertexPoints, the 2D coordinates of the vertices of every loop in any frame of the
        /// plane in which the outer loop runs counter-clockwise (GetLogicalLoopPoints). The sums and the bound are
        /// multiplied by the largest denominator of the loops (GetDenominator), 4 with a round hole, whose width is a
        /// quarter of its elements across both axes: each of its edges is listed once at the denominator 4. None without
        /// holes.
        /// With elementSize above 0, a hole whose walls to both sides are thinner than elementSize is also held to a
        /// cell on each side, the path less the width at most twice the bound, so that the hole follows the counts of
        /// the outer loop; and the corner rows: a corner of a hole closer than elementSize to an end of the
        /// outer loop in both directions leaves a single cell between them, all four of its nodes on the boundary, which
        /// is a dart unless the elements along the two edges at the end are shorter than CornerElementFactor times the
        /// distance of the hole corner along them; each such edge must then carry at least as many elements.
        /// </summary>
        public List<SubmapSeparation> GetRoomRows(double[][][] loopVertexPoints, int bound, double elementSize)
        {
            List<SubmapSeparation> rows = new List<SubmapSeparation>();
            if (!HasHoles) return rows;
            double[][][] points = GetLogicalLoopPoints(loopVertexPoints);
            if (points == null) return rows;
            SubmapFace[] loops = Loops;
            int denominator = GetAllDenominator();
            bound *= denominator;
            //
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int[] multiplicities = GetLogicalMultiplicities(denominator);
            int n = edgeIds.Length;
            double[][] outer = points[0];
            List<int> sideStarts = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (directions[k] != directions[(k + n - 1) % n]) sideStarts.Add(k);
            }
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (var point in outer)
            {
                minX = Math.Min(minX, point[0]);
                maxX = Math.Max(maxX, point[0]);
                minY = Math.Min(minY, point[1]);
                maxY = Math.Max(maxY, point[1]);
            }
            double tolerance = SeparationTolerance * Math.Max(maxX - minX, maxY - minY);
            // Every side by its axis, its coordinate across and its extent along
            int numOfSides = sideStarts.Count;
            int[] axes = new int[numOfSides];
            double[] across = new double[numOfSides];
            double[] alongMin = new double[numOfSides];
            double[] alongMax = new double[numOfSides];
            for (int s = 0; s < numOfSides; s++)
            {
                int start = sideStarts[s];
                int end = sideStarts[(s + 1) % numOfSides];
                axes[s] = directions[start] % 2;
                alongMin[s] = double.MaxValue;
                alongMax[s] = double.MinValue;
                int numOfPoints = 0;
                for (int k = start; ; k = (k + 1) % n)
                {
                    across[s] += outer[k][1 - axes[s]];
                    alongMin[s] = Math.Min(alongMin[s], outer[k][axes[s]]);
                    alongMax[s] = Math.Max(alongMax[s], outer[k][axes[s]]);
                    numOfPoints++;
                    if (k == end) break;
                }
                across[s] /= numOfPoints;
            }
            // Every hole by its extents and its first edge
            int numOfHoles = loops.Length - 1;
            double[][] holeMin = new double[numOfHoles][];
            double[][] holeMax = new double[numOfHoles][];
            for (int h = 0; h < numOfHoles; h++)
            {
                holeMin[h] = new double[] { double.MaxValue, double.MaxValue };
                holeMax[h] = new double[] { double.MinValue, double.MinValue };
                foreach (var point in points[h + 1])
                {
                    for (int c = 0; c < 2; c++)
                    {
                        holeMin[h][c] = Math.Min(holeMin[h][c], point[c]);
                        holeMax[h][c] = Math.Max(holeMax[h][c], point[c]);
                    }
                }
            }
            //
            for (int a = 0; a < numOfSides; a++)
            {
                for (int b = 0; b < numOfSides; b++)
                {
                    if (axes[a] != axes[b] || across[b] - across[a] <= tolerance) continue;
                    int axis = axes[a];
                    double commonMin = Math.Max(alongMin[a], alongMin[b]);
                    double commonMax = Math.Min(alongMax[a], alongMax[b]);
                    if (commonMax - commonMin <= tolerance) continue;
                    // The holes between the two sides within their common extent
                    List<int> between = new List<int>();
                    for (int h = 0; h < numOfHoles; h++)
                    {
                        if (holeMin[h][1 - axis] <= across[a] + tolerance || holeMax[h][1 - axis] >= across[b] - tolerance)
                            continue;
                        if (Math.Min(commonMax, holeMax[h][axis]) - Math.Max(commonMin, holeMin[h][axis]) <= tolerance)
                            continue;
                        between.Add(h);
                    }
                    if (between.Count == 0) continue;
                    // The lattice coordinate across grows along +j for sides along i and along +i for sides along j
                    int towards = axis == 0 ? 1 : 0;
                    List<int> towardsEdgeIds = new List<int>();
                    List<int> backEdgeIds = new List<int>();
                    for (int k = sideStarts[a]; k != sideStarts[b]; k = (k + 1) % n)
                    {
                        for (int m = 0; m < multiplicities[k]; m++)
                        {
                            if (directions[k] == towards) towardsEdgeIds.Add(edgeIds[k]);
                            else if (directions[k] == towards + 2) backEdgeIds.Add(edgeIds[k]);
                        }
                    }
                    foreach (var h in between)
                    {
                        SubmapSeparation row = GetRoomRow(towardsEdgeIds, backEdgeIds, new int[] { h }, towards,
                                                          denominator, 2 * bound, edgeIds[sideStarts[a]],
                                                          edgeIds[sideStarts[b]]);
                        rows.Add(row);
                        // Walls on both sides thinner than an element are a cell each
                        if (elementSize > 0 && holeMin[h][1 - axis] - across[a] < elementSize &&
                            across[b] - holeMax[h][1 - axis] < elementSize)
                        {
                            SubmapSeparation thinRow = new SubmapSeparation(_faceId, row.BackEdgeIds, row.TowardsEdgeIds,
                                                                            -2 * bound, row.FirstSideEdgeId,
                                                                            row.SecondSideEdgeId, row.HoleEdgeIds);
                            thinRow.IsThinWallRow = true;
                            rows.Add(thinRow);
                        }
                    }
                    for (int i = 0; i < between.Count; i++)
                    {
                        for (int j = i + 1; j < between.Count; j++)
                        {
                            int h1 = between[i];
                            int h2 = between[j];
                            if (holeMax[h1][1 - axis] >= holeMin[h2][1 - axis] - tolerance &&
                                holeMax[h2][1 - axis] >= holeMin[h1][1 - axis] - tolerance) continue;
                            double overlapMin = Math.Max(commonMin, Math.Max(holeMin[h1][axis], holeMin[h2][axis]));
                            double overlapMax = Math.Min(commonMax, Math.Min(holeMax[h1][axis], holeMax[h2][axis]));
                            if (overlapMax - overlapMin <= tolerance) continue;
                            rows.Add(GetRoomRow(towardsEdgeIds, backEdgeIds, new int[] { h1, h2 }, towards, denominator,
                                                3 * bound, edgeIds[sideStarts[a]], edgeIds[sideStarts[b]]));
                        }
                    }
                }
            }
            if (elementSize > 0) rows.AddRange(GetCornerRows(points, denominator, elementSize));
            return rows;
        }
        /// <summary>
        /// The corner rows of GetRoomRows over the logical points of the loops in the frame of the outer loop. Only an
        /// end between two edges that do not turn in their middle is a corner of the outer loop, and only the four corners
        /// of its square are corners of a round hole.
        /// </summary>
        private List<SubmapSeparation> GetCornerRows(double[][][] points, int denominator, double elementSize)
        {
            List<SubmapSeparation> rows = new List<SubmapSeparation>();
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int[] edgeTurns = new int[edgeIds.Length];
            for (int k = 0, logical = 0; k < _edgeIds.Length; k++)
            {
                for (int p = 0; p < GetNumberOfPieces(k); p++) edgeTurns[logical++] = GetEdgeTurn(k);
            }
            int unit = denominator;
            int n = edgeIds.Length;
            double[][] outer = points[0];
            Dictionary<int, int> edgeIdBound = new Dictionary<int, int>();
            Dictionary<int, int> edgeIdHole = new Dictionary<int, int>();
            for (int k = 0; k < n; k++)
            {
                int previous = (k + n - 1) % n;
                if ((directions[k] - directions[previous] + 4) % 4 != 1) continue;
                if (edgeTurns[k] != 0 || edgeTurns[previous] != 0) continue;
                double[] corner = outer[k];
                double[] forward = new double[] { DirectionX[directions[k]], DirectionY[directions[k]] };
                double[] backward = new double[] { -DirectionX[directions[previous]], -DirectionY[directions[previous]] };
                double forwardLength = Distance(outer[k], outer[(k + 1) % n]);
                double backwardLength = Distance(outer[previous], outer[k]);
                for (int h = 1; h < points.Length; h++)
                {
                    int numOfCorners = _holes[h - 1]._isRound ? 4 : points[h].Length;
                    for (int c = 0; c < numOfCorners; c++)
                    {
                        double[] point = points[h][c];
                        double dx = point[0] - corner[0];
                        double dy = point[1] - corner[1];
                        double alongForward = dx * forward[0] + dy * forward[1];
                        double alongBackward = dx * backward[0] + dy * backward[1];
                        if (!(alongForward > 0 && alongForward < elementSize && alongBackward > 0 &&
                              alongBackward < elementSize)) continue;
                        AddCornerBound(edgeIdBound, edgeIdHole, edgeIds[k],
                                       (int)Math.Ceiling(forwardLength / (CornerElementFactor * alongForward) - 1E-9),
                                       _holes[h - 1]._edgeIds[0]);
                        AddCornerBound(edgeIdBound, edgeIdHole, edgeIds[previous],
                                       (int)Math.Ceiling(backwardLength / (CornerElementFactor * alongBackward) - 1E-9),
                                       _holes[h - 1]._edgeIds[0]);
                    }
                }
            }
            foreach (var entry in edgeIdBound)
            {
                int[] towardsEdgeIds = new int[unit];
                for (int m = 0; m < unit; m++) towardsEdgeIds[m] = entry.Key;
                SubmapSeparation row = new SubmapSeparation(_faceId, towardsEdgeIds, new int[0], unit * entry.Value,
                                                            entry.Key, entry.Key, new int[] { edgeIdHole[entry.Key] });
                row.IsCornerRow = true;
                rows.Add(row);
            }
            return rows;
        }
        private static void AddCornerBound(Dictionary<int, int> edgeIdBound, Dictionary<int, int> edgeIdHole, int edgeId,
                                           int bound, int holeEdgeId)
        {
            int current;
            if (edgeIdBound.TryGetValue(edgeId, out current) && current >= bound) return;
            edgeIdBound[edgeId] = bound;
            edgeIdHole[edgeId] = holeEdgeId;
        }
        private static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]));
        }
        /// <summary>
        /// A row of GetRoomRows: the path between the two sides of the outer loop less the widths of the holes, the
        /// logical edges of each that run in the direction towards, or all edges of a round hole at a quarter of the
        /// denominator.
        /// </summary>
        private SubmapSeparation GetRoomRow(List<int> towardsEdgeIds, List<int> backEdgeIds, int[] holes, int towards,
                                            int denominator, int bound, int firstSideEdgeId, int secondSideEdgeId)
        {
            List<int> lessEdgeIds = new List<int>(backEdgeIds);
            int[] holeEdgeIds = new int[holes.Length];
            for (int i = 0; i < holes.Length; i++)
            {
                SubmapFace hole = _holes[holes[i]];
                holeEdgeIds[i] = hole._edgeIds[0];
                if (hole._isRound)
                {
                    foreach (var edgeId in hole._edgeIds)
                    {
                        for (int m = 0; m < denominator / 4; m++) lessEdgeIds.Add(edgeId);
                    }
                    continue;
                }
                int[] directions = hole.LogicalEdgeDirections;
                int[] edgeIds = hole.LogicalEdgeIds;
                int[] multiplicities = hole.GetLogicalMultiplicities(denominator);
                for (int k = 0; k < edgeIds.Length; k++)
                {
                    if (directions[k] != towards) continue;
                    for (int m = 0; m < multiplicities[k]; m++) lessEdgeIds.Add(edgeIds[k]);
                }
            }
            return new SubmapSeparation(_faceId, towardsEdgeIds.ToArray(), lessEdgeIds.ToArray(), bound, firstSideEdgeId,
                                        secondSideEdgeId, holeEdgeIds);
        }
        /// <summary>
        /// The lengths of the logical edges, a half having half the length of its edge and the pieces of an edge that
        /// turns twice a quarter, a half and a quarter; a quarter of the length around a round hole for each of its four.
        /// </summary>
        public double[] GetLogicalEdgeLengths(double[] edgeLengths)
        {
            List<double> lengths = new List<double>();
            if (_isRound)
            {
                double total = 0;
                foreach (var length in edgeLengths) total += length;
                for (int p = 0; p < 4; p++) lengths.Add(0.25 * total);
                return lengths.ToArray();
            }
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                int turn = Math.Abs(GetEdgeTurn(k));
                if (turn == 0) lengths.Add(edgeLengths[k]);
                else if (turn == 1)
                {
                    lengths.Add(0.5 * edgeLengths[k]);
                    lengths.Add(0.5 * edgeLengths[k]);
                }
                else
                {
                    lengths.Add(0.25 * edgeLengths[k]);
                    lengths.Add(0.5 * edgeLengths[k]);
                    lengths.Add(0.25 * edgeLengths[k]);
                }
            }
            return lengths.ToArray();
        }
        /// <summary>
        /// How many times every logical edge is listed in the sums of its sides at the given denominator, 1, 2 or 4 and
        /// at least the one of the loop: an edge that does not turn as many times as the denominator, a half half as
        /// many and of an edge that turns twice the quarters a quarter and the middle a half as many.
        /// </summary>
        private int[] GetLogicalMultiplicities(int denominator)
        {
            List<int> multiplicities = new List<int>();
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                int turn = Math.Abs(GetEdgeTurn(k));
                if (turn == 0) multiplicities.Add(denominator);
                else if (turn == 1)
                {
                    multiplicities.Add(denominator / 2);
                    multiplicities.Add(denominator / 2);
                }
                else
                {
                    multiplicities.Add(denominator / 4);
                    multiplicities.Add(denominator / 2);
                    multiplicities.Add(denominator / 4);
                }
            }
            return multiplicities.ToArray();
        }
    }
}
