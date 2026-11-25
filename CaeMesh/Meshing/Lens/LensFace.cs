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
    /// A face of one boundary loop, meshed by GmshLensMesher: with exactly two corners - a lens, a half disc, a dome
    /// cut in half through its pole, a wedge of a sphere between two meridians -, with one corner - a teardrop - or
    /// with no corner - a disc bounded by one closed edge or by several, an ellipse, a stadium, a rounded polygon, a
    /// half sphere between two meridians that meet at its poles. A two-sided face is split by one inner line from a
    /// node of one side to a node of the other into two three-sided pieces, each three blocks of quads
    /// (ThreeBlockMapping), so its grid has two inner nodes of three quads. A face with no corner is first split by a
    /// line between two nodes of its loop into two two-sided faces, four inner nodes of three quads. A face with one
    /// corner is cut by a line across the corner into a three-sided piece at the corner and a two-sided rest, three
    /// inner nodes of three quads. A face whose surface can have a pole may also have one reentrant vertex - a dome or
    /// a cone sector of more than 225 degrees with its pole - and then two corners or more: when the vertex is a pole
    /// of the surface it is split there by lines from the pole into pieces of about a right angle at it. The loop is
    /// stored from the first corner, vertex k being where edge k - 1 ends and edge k starts; side 0 of a two-sided face
    /// runs from corner 0 to corner 1, side 1 back to corner 0. Only a two-sided face has sides.
    /// </summary>
    [PmxType(2152)]
    public class LensFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int[] _vertexNodeIds;
        [PmxKey(2)] private int[] _edgeIds;
        [PmxKey(3)] private int[] _cornerIndices;
        [PmxKey(4)] private int[] _reentrantIndices;
        /// <summary>
        /// The fewest elements on a side: each piece of a side is a side of three blocks of quads and carries two at
        /// least.
        /// </summary>
        public const int MinNumberOfElementsPerSide = 4;
        /// <summary>
        /// The fewest elements on the loop of a face with one corner or none: the first line splits the loop into two
        /// parts, each a side of a two-sided face or of a piece, which carry MinNumberOfElementsPerSide each.
        /// </summary>
        public const int MinNumberOfElementsInLoop = 8;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the loop from corner 0: vertex k is where edge k - 1 ends and edge k starts.
        /// </summary>
        public int[] VertexNodeIds { get { return _vertexNodeIds; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        /// <summary>
        /// The indices of the corners in the loop: 0 and the index of corner 1, 0 alone for a face with one corner,
        /// and none for a face with no corner.
        /// </summary>
        public int[] CornerIndices { get { return _cornerIndices; } }
        /// <summary>
        /// The indices in the loop of the reentrant vertex, one at most; none for most faces.
        /// </summary>
        public int[] ReentrantIndices { get { return _reentrantIndices ?? new int[0]; } }
        /// <summary>
        /// The number of corners, 0, 1 or 2, or more with a reentrant vertex; only a face of two has sides.
        /// </summary>
        public int NumberOfSides { get { return _cornerIndices.Length; } }
        public int[] AllEdgeIds { get { return _edgeIds.ToArray(); } }
        /// <summary>
        /// The loop index of the vertex side k of a two-sided face starts at; none for another face.
        /// </summary>
        public int[] SideStartIndices
        {
            get { return _cornerIndices.Length == 2 ? _cornerIndices.ToArray() : new int[0]; }
        }
        /// <summary>
        /// The edge ids of the two sides of a two-sided face, each in the order the side runs; none for another face.
        /// </summary>
        public int[][] SideEdgeIds
        {
            get
            {
                if (_cornerIndices.Length != 2) return new int[0][];
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
        public LensFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] cornerIndices, int[] reentrantIndices)
        {
            _faceId = faceId;
            _vertexNodeIds = vertexNodeIds;
            _edgeIds = edgeIds;
            _cornerIndices = cornerIndices;
            _reentrantIndices = reentrantIndices;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Why the numbers of elements of the edges cannot give the face its grid, null when they can: the edges carry
        /// an odd number in all, a side of a two-sided face fewer than MinNumberOfElementsPerSide, or the loop of
        /// another face fewer than MinNumberOfElementsInLoop.
        /// </summary>
        public string GetCountReason(Dictionary<int, int> edgeIdCount)
        {
            int total = _edgeIds.Sum(edgeId => edgeIdCount[edgeId]);
            if (total % 2 != 0) return "its edges carry " + total + " elements in all, which must be even";
            if (NumberOfSides != 2 && total < MinNumberOfElementsInLoop)
                return "its edges carry " + total + " elements in all, fewer than " + MinNumberOfElementsInLoop;
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
