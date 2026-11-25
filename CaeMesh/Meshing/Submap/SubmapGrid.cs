// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    /// <summary>
    /// The lattice of quads SubmapMapping lays over a face whose corners are all near multiples of 90 degrees. The nodes
    /// are numbered along the boundary loops first, the outer loop and then the holes, node i of edge k being
    /// GetBoundaryNodeId(k, i) with the edges of all loops numbered one after another, and then the inner nodes row by
    /// row, inner node n being NumberOfBoundaryNodes + n. Every node has integer logical coordinates i, j; the quads are
    /// the unit cells of the lattice inside the outline and turn the way the outer loop turns in the parameter space.
    /// </summary>
    public class SubmapGrid
    {
        // Variables                                                                                                                
        private int[] _edgeDirections;
        private int[] _edgeCounts;
        private int[] _edgeOffsets;
        private int[] _nextEdges;
        private int[] _loopNumOfEdges;
        private int _numOfBoundaryNodes;
        private int[][] _logicalCoordinates;
        private double[][] _innerUv;
        private double[][] _innerXyz;
        private int[][] _quads;
        private int _numOfFoldedQuads;
        private double _minScaledJacobian;


        // Properties                                                                                                               
        /// <summary>
        /// The logical direction of every edge of all loops: 0, 1, 2 and 3 for +i, +j, -i and -j.
        /// </summary>
        public int[] EdgeDirections { get { return _edgeDirections; } }
        /// <summary>
        /// The number of elements on every edge.
        /// </summary>
        public int[] EdgeCounts { get { return _edgeCounts; } }
        /// <summary>
        /// The number of edges of every loop, the outer loop first.
        /// </summary>
        public int[] LoopNumberOfEdges { get { return _loopNumOfEdges; } }
        /// <summary>
        /// The number of boundary nodes on the outer loop, which come first.
        /// </summary>
        public int NumberOfOuterBoundaryNodes
        {
            get
            {
                int numOfNodes = 0;
                for (int k = 0; k < _loopNumOfEdges[0]; k++) numOfNodes += _edgeCounts[k];
                return numOfNodes;
            }
        }
        public int NumberOfBoundaryNodes { get { return _numOfBoundaryNodes; } }
        public int NumberOfInnerNodes { get { return _innerUv.Length; } }
        /// <summary>
        /// The logical coordinates i, j of every node by its id, the smallest of each being 0.
        /// </summary>
        public int[][] LogicalCoordinates { get { return _logicalCoordinates; } }
        public double[][] InnerUv { get { return _innerUv; } }
        public double[][] InnerXyz { get { return _innerXyz; } }
        /// <summary>
        /// The four node ids of every quad, counter-clockwise in the logical coordinates.
        /// </summary>
        public int[][] Quads { get { return _quads; } }
        /// <summary>
        /// The quads that turn against the boundary loop in the parameter space, or have a corner without area; the
        /// grid is valid when there are none.
        /// </summary>
        public int NumberOfFoldedQuads { get { return _numOfFoldedQuads; } }
        /// <summary>
        /// The smallest sine of a quad corner in the frame the grid was mapped in, negative where a quad folds; 1 for a
        /// grid of rectangles.
        /// </summary>
        public double MinScaledJacobian { get { return _minScaledJacobian; } }


        // Constructor                                                                                                              
        public SubmapGrid(int[] edgeDirections, int[] edgeCounts, int[] loopNumOfEdges, int[][] logicalCoordinates,
                          double[][] innerUv, double[][] innerXyz, int[][] quads, int numOfFoldedQuads,
                          double minScaledJacobian)
        {
            _edgeDirections = edgeDirections;
            _edgeCounts = edgeCounts;
            _edgeOffsets = new int[edgeCounts.Length];
            _numOfBoundaryNodes = 0;
            for (int k = 0; k < edgeCounts.Length; k++)
            {
                _edgeOffsets[k] = _numOfBoundaryNodes;
                _numOfBoundaryNodes += edgeCounts[k];
            }
            _loopNumOfEdges = loopNumOfEdges;
            _nextEdges = new int[edgeCounts.Length];
            int loopStart = 0;
            foreach (var numOfEdges in loopNumOfEdges)
            {
                for (int k = loopStart; k < loopStart + numOfEdges; k++) _nextEdges[k] = k + 1;
                _nextEdges[loopStart + numOfEdges - 1] = loopStart;
                loopStart += numOfEdges;
            }
            _logicalCoordinates = logicalCoordinates;
            _innerUv = innerUv;
            _innerXyz = innerXyz;
            _quads = quads;
            _numOfFoldedQuads = numOfFoldedQuads;
            _minScaledJacobian = minScaledJacobian;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The id of node i of edge k, which runs from its loop vertex (i = 0) to the next one of its loop
        /// (i = EdgeCounts[k]).
        /// </summary>
        public int GetBoundaryNodeId(int k, int i)
        {
            if (i == _edgeCounts[k]) return _edgeOffsets[_nextEdges[k]];
            return _edgeOffsets[k] + i;
        }
        /// <summary>
        /// The edge and the index on it of a boundary node id, or false for an inner node.
        /// </summary>
        public bool TryGetBoundaryNode(int nodeId, out int k, out int i)
        {
            k = 0;
            i = nodeId;
            while (k < _edgeCounts.Length && i >= _edgeCounts[k])
            {
                i -= _edgeCounts[k];
                k++;
            }
            return k < _edgeCounts.Length;
        }
    }
}
