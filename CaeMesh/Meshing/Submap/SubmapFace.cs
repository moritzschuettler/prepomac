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
    /// it needs an even number of them. The logical loop, in which every turn edge is replaced by its two halves, is
    /// the one SubmapMapping maps; the edges are counted, balanced and named by their geometric ids.
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
        private const double SeparationTolerance = 1E-3;


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
        /// The turn every edge makes in its middle: 1 to the left, -1 to the right and 0 for none; null for none at all.
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
        /// The direction of every logical edge, a turn edge giving two.
        /// </summary>
        public int[] LogicalEdgeDirections
        {
            get
            {
                List<int> directions = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    directions.Add(_edgeDirections[k]);
                    if (GetEdgeTurn(k) != 0) directions.Add((_edgeDirections[k] + GetEdgeTurn(k) + 4) % 4);
                }
                return directions.ToArray();
            }
        }
        /// <summary>
        /// The id of the edge every logical edge is, or is half of.
        /// </summary>
        public int[] LogicalEdgeIds
        {
            get
            {
                List<int> edgeIds = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    edgeIds.Add(_edgeIds[k]);
                    if (GetEdgeTurn(k) != 0) edgeIds.Add(_edgeIds[k]);
                }
                return edgeIds.ToArray();
            }
        }


        // Constructors                                                                                                             
        public SubmapFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] edgeDirections, double[] vertexAnglesDeg,
                          int[] edgeTurns)
        {
            _faceId = faceId;
            _vertexNodeIds = vertexNodeIds;
            _edgeIds = edgeIds;
            _edgeDirections = edgeDirections;
            _vertexAnglesDeg = vertexAnglesDeg;
            _edgeTurns = edgeTurns;
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
        /// The two pairs of sides whose numbers of elements must be equal for the logical outline to close: the edges
        /// along +i against those along -i, and the edges along +j against those along -j. With turn edges the sums are
        /// doubled, so that a half counts as its whole edge once: every edge that does not turn is listed twice in its
        /// side and every half once.
        /// </summary>
        public TransfiniteSidePair[] GetSidePairs()
        {
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int[] multiplicities = GetLogicalMultiplicities();
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
        /// The pairs of parallel sides that face each other across the face and must keep at least the bound of lattice
        /// cells between them, a side being a run of logical edges in one direction. Two sides are such a pair when, in
        /// the ideal outline the edge lengths draw (SubmapFaceDetector.GetIdealOutline), they lie apart and their
        /// extents along them overlap, both beyond a tolerance relative to the size of the outline. edgeLengths holds
        /// the length of every edge in loop order, a half having half the length of its edge. The pairs do not prove
        /// the lattice outline simple; they keep the order of the sides the face has. With turn edges the sums and the
        /// bound are doubled as in GetSidePairs.
        /// </summary>
        public List<SubmapSeparation> GetSeparations(double[] edgeLengths, int bound)
        {
            List<SubmapSeparation> separations = new List<SubmapSeparation>();
            int[] directions = LogicalEdgeDirections;
            int[] edgeIds = LogicalEdgeIds;
            int[] multiplicities = GetLogicalMultiplicities();
            double[] lengths = GetLogicalEdgeLengths(edgeLengths);
            if (HasTurnEdges) bound *= 2;
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
        /// half second when the count is odd.
        /// </summary>
        public int[] GetLogicalEdgeCounts(Dictionary<int, int> edgeIdCount)
        {
            List<int> counts = new List<int>();
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                int count = edgeIdCount[_edgeIds[k]];
                if (GetEdgeTurn(k) == 0) counts.Add(count);
                else
                {
                    counts.Add(count / 2);
                    counts.Add(count - count / 2);
                }
            }
            return counts.ToArray();
        }
        /// <summary>
        /// The first turn edge with an odd number of elements, which cannot be split into two halves, or -1 for none.
        /// </summary>
        public int GetOddTurnEdgeId(Dictionary<int, int> edgeIdCount)
        {
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                if (GetEdgeTurn(k) != 0 && edgeIdCount[_edgeIds[k]] % 2 != 0) return _edgeIds[k];
            }
            return -1;
        }
        /// <summary>
        /// The lengths of the logical edges, a half having half the length of its edge.
        /// </summary>
        public double[] GetLogicalEdgeLengths(double[] edgeLengths)
        {
            List<double> lengths = new List<double>();
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                if (GetEdgeTurn(k) == 0) lengths.Add(edgeLengths[k]);
                else
                {
                    lengths.Add(0.5 * edgeLengths[k]);
                    lengths.Add(0.5 * edgeLengths[k]);
                }
            }
            return lengths.ToArray();
        }
        /// <summary>
        /// How many times every logical edge is listed in the sums of its sides: once without turn edges, and with them
        /// twice for an edge that does not turn and once for a half.
        /// </summary>
        private int[] GetLogicalMultiplicities()
        {
            bool hasTurnEdges = HasTurnEdges;
            List<int> multiplicities = new List<int>();
            for (int k = 0; k < _edgeIds.Length; k++)
            {
                if (GetEdgeTurn(k) == 0) multiplicities.Add(hasTurnEdges ? 2 : 1);
                else
                {
                    multiplicities.Add(1);
                    multiplicities.Add(1);
                }
            }
            return multiplicities.ToArray();
        }
    }
}
