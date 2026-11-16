// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// A face of one boundary loop with exactly two corners - a lens, a half disc, a dome cut in half through its pole,
    /// a wedge of a sphere between two meridians - or with no corner and exactly two vertices, a half sphere between
    /// two meridians that meet at its poles, meshed by GmshLensMesher. A two-sided face is split by one inner line from
    /// a node of one side to a node of the other into two three-sided pieces, each three blocks of quads
    /// (ThreeBlockMapping), so its grid has two inner nodes of three quads; a face with no corner is first split by a
    /// line between its two vertices into two two-sided faces, four inner nodes of three quads. The loop is stored from
    /// the first corner, vertex k being where edge k - 1 ends and edge k starts; side 0 runs from corner 0 to corner 1,
    /// side 1 back to corner 0. A face with no corner has two vertices, two edges and its sides are its edges.
    /// </summary>
    [PmxType(2152)]
    public class LensFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int[] _vertexNodeIds;
        [PmxKey(2)] private int[] _edgeIds;
        [PmxKey(3)] private int[] _cornerIndices;
        /// <summary>
        /// The fewest elements on a side: each piece of a side is a side of three blocks of quads and carries two at
        /// least.
        /// </summary>
        public const int MinNumberOfElementsPerSide = 4;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the loop from corner 0: vertex k is where edge k - 1 ends and edge k starts.
        /// </summary>
        public int[] VertexNodeIds { get { return _vertexNodeIds; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        /// <summary>
        /// The indices of the corners in the loop, 0 and the index of corner 1; empty for a face with no corner.
        /// </summary>
        public int[] CornerIndices { get { return _cornerIndices; } }
        /// <summary>
        /// 2, or 0 for a face with no corner.
        /// </summary>
        public int NumberOfSides { get { return _cornerIndices.Length; } }
        public int[] AllEdgeIds { get { return _edgeIds.ToArray(); } }
        /// <summary>
        /// The loop index of the vertex side k starts at.
        /// </summary>
        public int[] SideStartIndices
        {
            get { return _cornerIndices.Length == 2 ? _cornerIndices.ToArray() : new int[] { 0, 1 }; }
        }
        /// <summary>
        /// The edge ids of the two sides, each in the order the side runs.
        /// </summary>
        public int[][] SideEdgeIds
        {
            get
            {
                int[] starts = SideStartIndices;
                int n = _edgeIds.Length;
                int[][] sides = new int[2][];
                for (int k = 0; k < 2; k++)
                {
                    int end = k == 0 ? starts[1] : n;
                    sides[k] = Enumerable.Range(starts[k], end - starts[k]).Select(i => _edgeIds[i]).ToArray();
                }
                return sides;
            }
        }


        // Constructor                                                                                                              
        public LensFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] cornerIndices)
        {
            _faceId = faceId;
            _vertexNodeIds = vertexNodeIds;
            _edgeIds = edgeIds;
            _cornerIndices = cornerIndices;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Why the numbers of elements of the edges cannot give the face its grid, null when they can: the edges carry
        /// an odd number in all, or a side fewer than MinNumberOfElementsPerSide.
        /// </summary>
        public string GetCountReason(Dictionary<int, int> edgeIdCount)
        {
            int total = _edgeIds.Sum(edgeId => edgeIdCount[edgeId]);
            if (total % 2 != 0) return "its edges carry " + total + " elements in all, which must be even";
            foreach (var side in SideEdgeIds)
            {
                int count = side.Sum(edgeId => edgeIdCount[edgeId]);
                if (count < MinNumberOfElementsPerSide)
                    return "its side " + (side.Length == 1 ? "Edge " + side[0] : "from Edge " + side[0] + " to Edge " +
                           side[side.Length - 1]) + " carries " + count + " elements, fewer than " +
                           MinNumberOfElementsPerSide;
            }
            return null;
        }
    }
}
