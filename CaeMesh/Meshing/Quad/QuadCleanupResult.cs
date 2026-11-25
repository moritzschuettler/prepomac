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
    /// The mesh QuadCleanup made of a face and what it did. The nodes keep the ids of the input: Points holds every node,
    /// moved or not, and IsNodeUsed tells the nodes the elements still use; an inner node is removed by a doublet or a
    /// collapse; the nodes the cleanup adds, to split a hexagon or a path of elements or inside a re-meshed region,
    /// follow the nodes of the input.
    /// </summary>
    public class QuadCleanupResult
    {
        // Variables                                                                                                                
        private readonly int[][] _elements;
        private readonly double[][] _points;
        private readonly bool[] _isNodeUsed;
        private readonly int _numOfTrianglesBefore;
        private readonly int _numOfTriangles;
        private readonly int _numOfFoldedElements;
        private readonly double _minScaledJacobianBefore;
        private readonly double _minScaledJacobian;
        private readonly int _numOfIrregularNodesBefore;
        private readonly int _numOfIrregularNodes;
        private readonly int _numOfPairs;
        private readonly int _numOfDoublets;
        private readonly int _numOfCollapses;
        private readonly int _numOfRotations;
        private readonly int _numOfRemeshedRegions;
        private readonly int _numOfSplitPaths;
        private readonly double _seconds;


        // Properties                                                                                                               
        /// <summary>The quads and the triangles left, of 4 and 3 node ids, turning the way the input turned.</summary>
        public int[][] Elements { get { return _elements; } }
        /// <summary>
        /// The positions of all the nodes of the input, the free ones where the cleanup left them, followed by the nodes
        /// the cleanup added.
        /// </summary>
        public double[][] Points { get { return _points; } }
        public bool[] IsNodeUsed { get { return _isNodeUsed; } }
        public int NumberOfTrianglesBefore { get { return _numOfTrianglesBefore; } }
        public int NumberOfTriangles { get { return _numOfTriangles; } }
        public int NumberOfQuads { get { return _elements.Length - _numOfTriangles; } }
        /// <summary>The elements of which a corner folds or has no area.</summary>
        public int NumberOfFoldedElements { get { return _numOfFoldedElements; } }
        /// <summary>The worst scaled Jacobian of the input, a triangle scaled so that 60 degrees count as 1.</summary>
        public double MinScaledJacobianBefore { get { return _minScaledJacobianBefore; } }
        public double MinScaledJacobian { get { return _minScaledJacobian; } }
        /// <summary>The inner nodes that do not join four edges, in the input.</summary>
        public int NumberOfIrregularNodesBefore { get { return _numOfIrregularNodesBefore; } }
        public int NumberOfIrregularNodes { get { return _numOfIrregularNodes; } }
        /// <summary>The pairs of triangles turned into quads.</summary>
        public int NumberOfPairs { get { return _numOfPairs; } }
        public int NumberOfDoublets { get { return _numOfDoublets; } }
        public int NumberOfCollapses { get { return _numOfCollapses; } }
        public int NumberOfRotations { get { return _numOfRotations; } }
        /// <summary>The pairs of triangles whose region was meshed again with quads only, in the last passes.</summary>
        public int NumberOfRemeshedRegions { get { return _numOfRemeshedRegions; } }
        /// <summary>The pairs of triangles turned into quads by splitting the path of elements between them.</summary>
        public int NumberOfSplitPaths { get { return _numOfSplitPaths; } }
        public double Seconds { get { return _seconds; } }


        // Constructors                                                                                                             
        public QuadCleanupResult(int[][] elements, double[][] points, bool[] isNodeUsed, int numOfTrianglesBefore,
                                 int numOfTriangles, int numOfFoldedElements, double minScaledJacobianBefore,
                                 double minScaledJacobian, int numOfIrregularNodesBefore, int numOfIrregularNodes,
                                 int numOfPairs, int numOfDoublets, int numOfCollapses, int numOfRotations,
                                 int numOfRemeshedRegions, int numOfSplitPaths, double seconds)
        {
            _elements = elements;
            _points = points;
            _isNodeUsed = isNodeUsed;
            _numOfTrianglesBefore = numOfTrianglesBefore;
            _numOfTriangles = numOfTriangles;
            _numOfFoldedElements = numOfFoldedElements;
            _minScaledJacobianBefore = minScaledJacobianBefore;
            _minScaledJacobian = minScaledJacobian;
            _numOfIrregularNodesBefore = numOfIrregularNodesBefore;
            _numOfIrregularNodes = numOfIrregularNodes;
            _numOfPairs = numOfPairs;
            _numOfDoublets = numOfDoublets;
            _numOfCollapses = numOfCollapses;
            _numOfRotations = numOfRotations;
            _numOfRemeshedRegions = numOfRemeshedRegions;
            _numOfSplitPaths = numOfSplitPaths;
            _seconds = seconds;
        }
    }
}
