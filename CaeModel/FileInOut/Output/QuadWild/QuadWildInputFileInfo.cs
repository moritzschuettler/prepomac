// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace FileInOut.Output
{
    public class QuadWildInputFileInfo
    {
        // Variables
        private int _nodeCount;
        private int _faceCount;
        private int _sharpEdgeCount;
        private QuadWildModelEdge[] _modelEdges;
        // For each input model face the sorted ids of the model edges that bound it
        private int[][] _modelFaceEdgeIds;
        // Model edges shared by more than two faces of a shell compound, where QuadWild meshes detached sheets
        private int _seamEdgeCount;
        // Positions of the nodes that were split into copies for the detached sheets; the QuadWild output
        // vertices at these positions are merged again
        private double[][] _splitNodeCoordinates;


        // Properties
        public int NodeCount { get { return _nodeCount; } }
        public int FaceCount { get { return _faceCount; } }
        public int SharpEdgeCount { get { return _sharpEdgeCount; } }
        public QuadWildModelEdge[] ModelEdges { get { return _modelEdges; } }
        public int[][] ModelFaceEdgeIds { get { return _modelFaceEdgeIds; } }
        public int SeamEdgeCount { get { return _seamEdgeCount; } }
        public double[][] SplitNodeCoordinates { get { return _splitNodeCoordinates; } }


        // Constructors
        public QuadWildInputFileInfo(int nodeCount, int faceCount, int sharpEdgeCount,
                                     QuadWildModelEdge[] modelEdges, int[][] modelFaceEdgeIds,
                                     int seamEdgeCount, double[][] splitNodeCoordinates)
        {
            _nodeCount = nodeCount;
            _faceCount = faceCount;
            _sharpEdgeCount = sharpEdgeCount;
            _modelEdges = modelEdges;
            _modelFaceEdgeIds = modelFaceEdgeIds;
            _seamEdgeCount = seamEdgeCount;
            _splitNodeCoordinates = splitNodeCoordinates;
        }
    }
}
