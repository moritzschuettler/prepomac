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
    /// A face of exactly two boundary loops that run roughly parallel, meshed by RingMapping: both loops carry the same
    /// number of nodes, node i of the outer loop is joined to node i of the inner loop, turned by the shift RingMapping
    /// finds, and the layers between them are quads. Each loop is stored as its vertices and its edges in loop order,
    /// edge k running from vertex k to vertex k + 1; a closed edge is a loop of one vertex and one edge. The inner loop
    /// runs the same way round as the outer loop, not with the face on the same side.
    /// The edges of a paired ring face each other one to one: the loops have as many edges, and inner edge k faces outer
    /// edge k, so each pair carries the same number of elements and the nodes face each other along lines and arcs
    /// alike. Of a ring that is not paired only the totals of the two loops are equal.
    /// The face also keeps how far apart its loops are, the smallest distance and the mean distance between the points
    /// RingMapping joins, which tell a thin wall (IsThinWall) whatever the element size.
    /// </summary>
    [PmxType(2149)]
    public class RingFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int[] _outerVertexNodeIds;
        [PmxKey(2)] private int[] _outerEdgeIds;
        [PmxKey(3)] private int[] _innerVertexNodeIds;
        [PmxKey(4)] private int[] _innerEdgeIds;
        [PmxKey(5)] private bool _isPaired;
        [PmxKey(6)] private double _minLoopDistance;
        [PmxKey(7)] private double _meanLoopDistance;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the outer loop: vertex k is where edge k - 1 ends and edge k starts.
        /// </summary>
        public int[] OuterVertexNodeIds { get { return _outerVertexNodeIds; } }
        public int[] OuterEdgeIds { get { return _outerEdgeIds; } }
        /// <summary>
        /// The vertex node ids of the inner loop, which runs the same way round as the outer loop.
        /// </summary>
        public int[] InnerVertexNodeIds { get { return _innerVertexNodeIds; } }
        /// <summary>
        /// The edge ids of the inner loop; of a paired ring inner edge k faces outer edge k.
        /// </summary>
        public int[] InnerEdgeIds { get { return _innerEdgeIds; } }
        /// <summary>
        /// Whether every inner edge faces an outer edge one to one, so that each pair carries the same number of elements.
        /// </summary>
        public bool IsPaired { get { return _isPaired; } }
        /// <summary>
        /// The smallest distance between the two loops.
        /// </summary>
        public double MinLoopDistance { get { return _minLoopDistance; } }
        /// <summary>
        /// The mean distance between the points of the two loops RingMapping joins, which its number of layers follows.
        /// </summary>
        public double MeanLoopDistance { get { return _meanLoopDistance; } }
        /// <summary>
        /// The ids of the edges of both loops, the outer loop first.
        /// </summary>
        public int[] AllEdgeIds
        {
            get
            {
                int[] edgeIds = new int[_outerEdgeIds.Length + _innerEdgeIds.Length];
                Array.Copy(_outerEdgeIds, edgeIds, _outerEdgeIds.Length);
                Array.Copy(_innerEdgeIds, 0, edgeIds, _outerEdgeIds.Length, _innerEdgeIds.Length);
                return edgeIds;
            }
        }


        // Constructor                                                                                                              
        public RingFace(int faceId, int[] outerVertexNodeIds, int[] outerEdgeIds, int[] innerVertexNodeIds,
                        int[] innerEdgeIds, bool isPaired, double minLoopDistance, double meanLoopDistance)
        {
            _faceId = faceId;
            _outerVertexNodeIds = outerVertexNodeIds;
            _outerEdgeIds = outerEdgeIds;
            _innerVertexNodeIds = innerVertexNodeIds;
            _innerEdgeIds = innerEdgeIds;
            _isPaired = isPaired;
            _minLoopDistance = minLoopDistance;
            _meanLoopDistance = meanLoopDistance;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Whether both the smallest and mean distances between the loops are below RingFaceDetector.ThinWallFactor
        /// times the element size. A shell with a sharp-cornered lattice keeps that lattice even with a thin wall.
        /// </summary>
        public bool IsThinWall(double elementSize)
        {
            double limit = RingFaceDetector.ThinWallFactor * elementSize;
            return _minLoopDistance < limit && _meanLoopDistance < limit;
        }
        /// <summary>
        /// The sums the numbers of elements must make equal: of a paired ring every outer edge against the inner edge
        /// that faces it, otherwise the outer loop against the inner loop.
        /// </summary>
        public TransfiniteSidePair[] GetSidePairs()
        {
            if (!_isPaired)
                return new TransfiniteSidePair[] { new TransfiniteSidePair(_faceId, _outerEdgeIds, _innerEdgeIds) };
            TransfiniteSidePair[] pairs = new TransfiniteSidePair[_outerEdgeIds.Length];
            for (int k = 0; k < pairs.Length; k++)
                pairs[k] = new TransfiniteSidePair(_faceId, new int[] { _outerEdgeIds[k] }, new int[] { _innerEdgeIds[k] });
            return pairs;
        }
        /// <summary>
        /// Why the numbers of elements of the edges cannot give the ring its grid, null when they can: the two loops,
        /// or two edges that face each other, carry different numbers.
        /// </summary>
        public string GetUnequalCountReason(Dictionary<int, int> edgeIdCount)
        {
            foreach (var pair in GetSidePairs())
            {
                int sumA = 0;
                int sumB = 0;
                foreach (var edgeId in pair.SideA) sumA += edgeIdCount[edgeId];
                foreach (var edgeId in pair.SideB) sumB += edgeIdCount[edgeId];
                if (sumA == sumB) continue;
                if (_isPaired && _outerEdgeIds.Length > 1)
                    return "Edge " + pair.SideA[0] + " and Edge " + pair.SideB[0] + ", which face each other, carry " + sumA +
                           " and " + sumB + " elements, which must be equal";
                return "its outer loop carries " + sumA + " elements and its inner loop " + sumB + ", which must be equal";
            }
            return null;
        }
    }
}
