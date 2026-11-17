// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// The three blocks of quads ThreeBlockMapping lays over a three-sided face, or the one block FourSidedMapping
    /// lays over a four-sided face. The nodes are numbered along the boundary loop first, node i of side k being
    /// GetBoundaryNodeId(k, i), and then the inner nodes, inner node n being NumberOfBoundaryNodes + n. The quads turn
    /// the way the boundary loop turns in the parameter space.
    /// </summary>
    public class ThreeBlockGrid
    {
        // Variables
        private int[] _sideCounts;
        private double[][] _innerUv;
        private double[][] _innerXyz;
        private int[][] _quads;
        private int _numOfFoldedQuads;


        // Properties
        /// <summary>
        /// The number of elements on every side.
        /// </summary>
        public int[] SideCounts { get { return _sideCounts; } }
        public int NumberOfBoundaryNodes { get { return _sideCounts.Sum(); } }
        public double[][] InnerUv { get { return _innerUv; } }
        public double[][] InnerXyz { get { return _innerXyz; } }
        /// <summary>
        /// The four node ids of every quad.
        /// </summary>
        public int[][] Quads { get { return _quads; } }
        /// <summary>
        /// The quads that turn against the boundary loop in the parameter space, or have a corner without area; the
        /// grid is valid when there are none.
        /// </summary>
        public int NumberOfFoldedQuads { get { return _numOfFoldedQuads; } }


        // Constructors
        public ThreeBlockGrid(int[] sideCounts, double[][] innerUv, double[][] innerXyz, int[][] quads,
                              int numOfFoldedQuads)
        {
            _sideCounts = sideCounts;
            _innerUv = innerUv;
            _innerXyz = innerXyz;
            _quads = quads;
            _numOfFoldedQuads = numOfFoldedQuads;
        }


        // Methods
        /// <summary>
        /// The id of node i of side k, which runs from corner k (i = 0) to corner k + 1 (i = SideCounts[k]).
        /// </summary>
        public int GetBoundaryNodeId(int k, int i)
        {
            if (i == _sideCounts[k]) return k == _sideCounts.Length - 1 ? 0 : GetBoundaryNodeId(k + 1, 0);
            int id = i;
            for (int side = 0; side < k; side++) id += _sideCounts[side];
            return id;
        }
        /// <summary>
        /// The side and the index on it of a boundary node id, or false for an inner node.
        /// </summary>
        public bool TryGetBoundaryNode(int nodeId, out int k, out int i)
        {
            k = 0;
            i = nodeId;
            while (k < _sideCounts.Length && i >= _sideCounts[k])
            {
                i -= _sideCounts[k];
                k++;
            }
            return k < _sideCounts.Length;
        }
    }
}
