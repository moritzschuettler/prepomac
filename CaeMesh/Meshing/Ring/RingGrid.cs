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
    /// The quads RingMapping lays between the two loops of a ring face. The N nodes of the outer loop come first, node i
    /// being i, then the N nodes of the inner loop in the order they were given, node j being N + j, then the inner nodes
    /// layer by layer from the outer loop inwards, node i of layer l (1 to NumberOfLayers - 1) being 2N + (l - 1) N + i.
    /// Node i of the outer loop is joined to node (i + Shift) mod N of the inner loop. The quads turn the way the outer
    /// loop turns in the parameter space.
    /// </summary>
    public class RingGrid
    {
        // Variables                                                                                                                
        private int _numOfLoopNodes;
        private int _numOfLayers;
        private int _shift;
        private double[][] _innerUv;
        private double[][] _innerXyz;
        private int[][] _quads;
        private int _numOfFoldedQuads;
        private double _minScaledJacobian;


        // Properties                                                                                                               
        /// <summary>
        /// The number of nodes on each loop, N.
        /// </summary>
        public int NumberOfLoopNodes { get { return _numOfLoopNodes; } }
        /// <summary>
        /// The number of quads between the two loops along every line that joins them.
        /// </summary>
        public int NumberOfLayers { get { return _numOfLayers; } }
        /// <summary>
        /// The node of the inner loop that node 0 of the outer loop is joined to.
        /// </summary>
        public int Shift { get { return _shift; } }
        public int NumberOfBoundaryNodes { get { return 2 * _numOfLoopNodes; } }
        public int NumberOfInnerNodes { get { return _innerUv.Length; } }
        public double[][] InnerUv { get { return _innerUv; } }
        public double[][] InnerXyz { get { return _innerXyz; } }
        public int[][] Quads { get { return _quads; } }
        /// <summary>
        /// The quads that turn against the outer loop in the parameter space, or have a corner without area; the grid is
        /// valid when there are none.
        /// </summary>
        public int NumberOfFoldedQuads { get { return _numOfFoldedQuads; } }
        /// <summary>
        /// The smallest sine of a quad corner in the frame the grid was mapped in, negative where a quad folds.
        /// </summary>
        public double MinScaledJacobian { get { return _minScaledJacobian; } }


        // Constructor                                                                                                              
        public RingGrid(int numOfLoopNodes, int numOfLayers, int shift, double[][] innerUv, double[][] innerXyz,
                        int[][] quads, int numOfFoldedQuads, double minScaledJacobian)
        {
            _numOfLoopNodes = numOfLoopNodes;
            _numOfLayers = numOfLayers;
            _shift = shift;
            _innerUv = innerUv;
            _innerXyz = innerXyz;
            _quads = quads;
            _numOfFoldedQuads = numOfFoldedQuads;
            _minScaledJacobian = minScaledJacobian;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The id of node i of layer l: layer 0 is the outer loop and layer NumberOfLayers the inner loop, whose node i is
        /// the inner node joined to outer node i.
        /// </summary>
        public int GetNodeId(int l, int i)
        {
            int n = _numOfLoopNodes;
            i = ((i % n) + n) % n;
            if (l == 0) return i;
            if (l == _numOfLayers) return n + (i + _shift) % n;
            return 2 * n + (l - 1) * n + i;
        }
    }
}
