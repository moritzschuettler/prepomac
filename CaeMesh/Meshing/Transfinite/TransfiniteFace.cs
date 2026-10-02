// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    /// <summary>
    /// A face of a geometry part that is meshed by a structured grid: its three or four corners, in the order its
    /// boundary loop runs, and the edges of each side between them. A side is a chain of edges, joined where they
    /// meet at a flatter angle than the transfinite angle.
    /// </summary>
    [Serializable]
    public class TransfiniteFace
    {
        // Variables
        private int _faceId;
        private int[] _cornerNodeIds;
        private double[] _cornerAnglesDeg;
        private int[][] _sideEdgeIds;


        // Properties
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the corners in loop order. Side k runs from corner k to corner k + 1.
        /// </summary>
        public int[] CornerNodeIds { get { return _cornerNodeIds; } }
        public double[] CornerAnglesDeg { get { return _cornerAnglesDeg; } }
        /// <summary>
        /// The edge ids of every side, in the order the side runs from its first corner.
        /// </summary>
        public int[][] SideEdgeIds { get { return _sideEdgeIds; } }
        public int NumberOfSides { get { return _cornerNodeIds.Length; } }
        /// <summary>
        /// The corner a three-sided face moves its singular point towards, the sharpest one, or -1 when the face has
        /// four sides.
        /// </summary>
        public int SharpestCornerIndex
        {
            get
            {
                if (_cornerNodeIds.Length != 3) return -1;
                int sharpest = 0;
                for (int i = 1; i < _cornerAnglesDeg.Length; i++)
                {
                    if (_cornerAnglesDeg[i] < _cornerAnglesDeg[sharpest]) sharpest = i;
                }
                return sharpest;
            }
        }


        // Constructors
        public TransfiniteFace(int faceId, int[] cornerNodeIds, double[] cornerAnglesDeg, int[][] sideEdgeIds)
        {
            _faceId = faceId;
            _cornerNodeIds = cornerNodeIds;
            _cornerAnglesDeg = cornerAnglesDeg;
            _sideEdgeIds = sideEdgeIds;
        }
    }
}
