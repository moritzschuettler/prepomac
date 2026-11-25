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

namespace CaeMesh
{
    /// <summary>
    /// A face closed along exactly one seam that ends in a pole - a degenerate edge of the surface, the apex of a cone
    /// or the pole of a sphere - at one end and on a closed rim at the other (a dome, a spherical cap, a cone with its
    /// apex, a head of a pressure vessel), or in a pole at both ends with no rim (a sphere, a spheroid), meshed by
    /// GmshCapMesher as a structured cap, an O-grid: N elements round, rings of N quads from the rim inward, and around
    /// each pole a block of a x b quads, 2 (a + b) = N, whose boundary is the innermost ring. The seam is a line of the
    /// grid through the rings and into the block to the pole. The rim is stored as its vertices and its edges in loop
    /// order from its vertex on the seam, edge k running from vertex k to vertex k + 1; a closed edge is a rim of one
    /// vertex and one edge. The poles are stored by their vertex node ids, the one at the end of the seam away from the
    /// rim, or both ends of the seam of a face with no rim, the first one first.
    /// </summary>
    [PmxType(2151)]
    public class CapFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int _seamEdgeId;
        [PmxKey(2)] private int[] _rimVertexNodeIds;
        [PmxKey(3)] private int[] _rimEdgeIds;
        [PmxKey(4)] private int[] _poleVertexNodeIds;
        /// <summary>
        /// The fewest elements round a cap: a block of 2 x 2 quads closes each pole.
        /// </summary>
        public const int MinNumberOfElementsRound = 8;
        /// <summary>
        /// The most of the elements of a seam that is not set the blocks at its poles take.
        /// </summary>
        public const double MaxBlockShare = 0.45;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The edge the face is closed along, from vertex 0 of the rim, or from the first pole, to the last pole.
        /// </summary>
        public int SeamEdgeId { get { return _seamEdgeId; } }
        /// <summary>
        /// The vertex node ids of the rim from its vertex on the seam: vertex k is where edge k - 1 ends and edge k starts.
        /// Empty for a face with a pole at both ends of its seam.
        /// </summary>
        public int[] RimVertexNodeIds { get { return _rimVertexNodeIds; } }
        public int[] RimEdgeIds { get { return _rimEdgeIds; } }
        /// <summary>
        /// The vertex node ids of the poles: one at the end of the seam away from the rim, or the two ends of the seam.
        /// </summary>
        public int[] PoleVertexNodeIds { get { return _poleVertexNodeIds; } }
        public bool HasRim { get { return _rimEdgeIds.Length > 0; } }
        public int NumberOfPoles { get { return _poleVertexNodeIds.Length; } }
        /// <summary>
        /// The vertex node id the seam starts at: vertex 0 of the rim, or the first pole.
        /// </summary>
        public int SeamStartVertexNodeId { get { return HasRim ? _rimVertexNodeIds[0] : _poleVertexNodeIds[0]; } }
        public int SeamEndVertexNodeId { get { return _poleVertexNodeIds[_poleVertexNodeIds.Length - 1]; } }
        /// <summary>
        /// The ids of the edges of the rim and then the seam.
        /// </summary>
        public int[] AllEdgeIds { get { return _rimEdgeIds.Concat(new int[] { _seamEdgeId }).ToArray(); } }


        // Constructor                                                                                                              
        public CapFace(int faceId, int seamEdgeId, int[] rimVertexNodeIds, int[] rimEdgeIds, int[] poleVertexNodeIds)
        {
            _faceId = faceId;
            _seamEdgeId = seamEdgeId;
            _rimVertexNodeIds = rimVertexNodeIds;
            _rimEdgeIds = rimEdgeIds;
            _poleVertexNodeIds = poleVertexNodeIds;
        }


        // Static methods                                                                                                           
        /// <summary>
        /// The block that closes a pole of a cap of numOfElementsRound elements round, an even number of at least
        /// MinNumberOfElementsRound: a quads across the seam and b along it, 2 (a + b) = numOfElementsRound, as equal as it
        /// allows and b the larger, and the depth of the pole in the block, the elements of the seam inside it, half of b
        /// rounded down.
        /// </summary>
        public static void GetBlock(int numOfElementsRound, out int a, out int b, out int depth)
        {
            a = numOfElementsRound / 4;
            b = numOfElementsRound / 2 - a;
            depth = b / 2;
        }
        /// <summary>
        /// The number of elements of the seam of a cap that is not set: the natural one, the number the mesh size asks
        /// for, unless the blocks at the poles would take more than MaxBlockShare of it, as on a flat cap, then the fewest
        /// that keep them within, and at least one ring; even where it must be. It is never lowered: the shape of the block
        /// of a cone does not change with its size, and a lowered seam only stretches the rings along it.
        /// </summary>
        public static int GetSeamNumOfElements(int naturalCount, int numOfElementsRound, int numOfPoles, bool even)
        {
            int a;
            int b;
            int depth;
            GetBlock(numOfElementsRound, out a, out b, out depth);
            int blockElements = numOfPoles * depth;
            int fewest = Math.Max(blockElements + 1, (int)Math.Ceiling(blockElements / MaxBlockShare - 1E-9));
            int count = Math.Max(fewest, naturalCount);
            if (even && count % 2 != 0) count++;
            return count;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Why the numbers of elements of the edges cannot give the cap its grid, null when they can: the rim carries an
        /// odd number of elements or fewer than MinNumberOfElementsRound, or the seam leaves no ring between the rim and
        /// the blocks at the poles.
        /// </summary>
        public string GetCountReason(Dictionary<int, int> edgeIdCount, int numOfElementsRound)
        {
            if (HasRim)
            {
                numOfElementsRound = 0;
                foreach (var edgeId in _rimEdgeIds) numOfElementsRound += edgeIdCount[edgeId];
                if (numOfElementsRound % 2 != 0)
                    return "its rim carries " + numOfElementsRound + " elements, which must be even";
                if (numOfElementsRound < MinNumberOfElementsRound)
                    return "its rim carries " + numOfElementsRound + " elements, fewer than " + MinNumberOfElementsRound;
            }
            int a;
            int b;
            int depth;
            GetBlock(numOfElementsRound, out a, out b, out depth);
            int seamCount = edgeIdCount[_seamEdgeId];
            int minSeamCount = NumberOfPoles * depth + 1;
            if (seamCount < minSeamCount)
                return "its seam carries " + seamCount + " elements, which leaves no ring between " +
                       (HasRim ? "its rim and the block of " : "the blocks of ") + a + " x " + b + " quads at " +
                       (HasRim ? "its pole" : "its poles") + " (it needs " + minSeamCount + " at least)";
            return null;
        }
    }
}
