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
    /// A face closed along exactly one seam and bounded by exactly two closed rims, the seam running from one rim to the
    /// other - a full cylinder, a cone frustum, a pipe bend, a surface of revolution or extrusion of a closed profile -
    /// meshed by GmshSeamGridMesher as a structured tube: both rims carry the same number of nodes, node i of one is
    /// joined to node i of the other, counted from the seam the same way round, and the seam carries the layers. It is a
    /// ring that does not lie flat. Each rim is stored as its vertices and its edges in loop order from its vertex on the
    /// seam, edge k running from vertex k to vertex k + 1; a closed edge is a rim of one vertex and one edge. The second rim
    /// runs the same way round as the first.
    /// The edges of a paired tube face each other one to one: the rims have as many edges and their vertices lie at the
    /// same shares of the length round from the seam, so second edge k faces first edge k and each pair carries the same
    /// number of elements. Of a tube that is not paired only the totals of the two rims are equal.
    /// </summary>
    [PmxType(2150)]
    public class TubeFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int _seamEdgeId;
        [PmxKey(2)] private int[] _firstRimVertexNodeIds;
        [PmxKey(3)] private int[] _firstRimEdgeIds;
        [PmxKey(4)] private int[] _secondRimVertexNodeIds;
        [PmxKey(5)] private int[] _secondRimEdgeIds;
        [PmxKey(6)] private bool _isPaired;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The edge the face is closed along, from vertex 0 of the first rim to vertex 0 of the second.
        /// </summary>
        public int SeamEdgeId { get { return _seamEdgeId; } }
        /// <summary>
        /// The vertex node ids of the first rim from its vertex on the seam: vertex k is where edge k - 1 ends and edge k
        /// starts.
        /// </summary>
        public int[] FirstRimVertexNodeIds { get { return _firstRimVertexNodeIds; } }
        public int[] FirstRimEdgeIds { get { return _firstRimEdgeIds; } }
        /// <summary>
        /// The vertex node ids of the second rim from its vertex on the seam, the same way round as the first rim.
        /// </summary>
        public int[] SecondRimVertexNodeIds { get { return _secondRimVertexNodeIds; } }
        /// <summary>
        /// The edge ids of the second rim; of a paired tube second edge k faces first edge k.
        /// </summary>
        public int[] SecondRimEdgeIds { get { return _secondRimEdgeIds; } }
        /// <summary>
        /// Whether every edge of the second rim faces an edge of the first one to one, so that each pair carries the same
        /// number of elements.
        /// </summary>
        public bool IsPaired { get { return _isPaired; } }
        /// <summary>
        /// The ids of the edges of both rims, the first rim first.
        /// </summary>
        public int[] RimEdgeIds
        {
            get
            {
                int[] edgeIds = new int[_firstRimEdgeIds.Length + _secondRimEdgeIds.Length];
                Array.Copy(_firstRimEdgeIds, edgeIds, _firstRimEdgeIds.Length);
                Array.Copy(_secondRimEdgeIds, 0, edgeIds, _firstRimEdgeIds.Length, _secondRimEdgeIds.Length);
                return edgeIds;
            }
        }
        /// <summary>
        /// The ids of the edges of both rims and then the seam.
        /// </summary>
        public int[] AllEdgeIds
        {
            get
            {
                int[] rimEdgeIds = RimEdgeIds;
                int[] edgeIds = new int[rimEdgeIds.Length + 1];
                Array.Copy(rimEdgeIds, edgeIds, rimEdgeIds.Length);
                edgeIds[rimEdgeIds.Length] = _seamEdgeId;
                return edgeIds;
            }
        }


        // Constructor                                                                                                              
        public TubeFace(int faceId, int seamEdgeId, int[] firstRimVertexNodeIds, int[] firstRimEdgeIds,
                        int[] secondRimVertexNodeIds, int[] secondRimEdgeIds, bool isPaired)
        {
            _faceId = faceId;
            _seamEdgeId = seamEdgeId;
            _firstRimVertexNodeIds = firstRimVertexNodeIds;
            _firstRimEdgeIds = firstRimEdgeIds;
            _secondRimVertexNodeIds = secondRimVertexNodeIds;
            _secondRimEdgeIds = secondRimEdgeIds;
            _isPaired = isPaired;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The sums the numbers of elements must make equal: of a paired tube every edge of the first rim against the edge
        /// of the second that faces it, otherwise the first rim against the second.
        /// </summary>
        public TransfiniteSidePair[] GetSidePairs()
        {
            if (!_isPaired)
                return new TransfiniteSidePair[] { new TransfiniteSidePair(_faceId, _firstRimEdgeIds, _secondRimEdgeIds) };
            TransfiniteSidePair[] pairs = new TransfiniteSidePair[_firstRimEdgeIds.Length];
            for (int k = 0; k < pairs.Length; k++)
                pairs[k] = new TransfiniteSidePair(_faceId, new int[] { _firstRimEdgeIds[k] },
                                                   new int[] { _secondRimEdgeIds[k] });
            return pairs;
        }
        /// <summary>
        /// Why the numbers of elements of the edges cannot give the tube its grid, null when they can: the two rims, or
        /// two edges that face each other, carry different numbers.
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
                if (_isPaired && _firstRimEdgeIds.Length > 1)
                    return "Edge " + pair.SideA[0] + " and Edge " + pair.SideB[0] + ", which face each other, carry " + sumA +
                           " and " + sumB + " elements, which must be equal";
                return "its rims carry " + sumA + " and " + sumB + " elements, which must be equal";
            }
            return null;
        }
    }
}
