// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    public class QuadWildEdgeRecoveryInfo
    {
        // Variables
        private bool _success;
        private int _edgeCount;
        private int _segmentCount;
        private int _freeEdgeCount;
        private int _faceCount;
        private bool _usedFallback;
        private string _warning;
        private string _faceWarning;


        // Properties
        public bool Success { get { return _success; } }
        public int EdgeCount { get { return _edgeCount; } }
        public int SegmentCount { get { return _segmentCount; } }
        public int FreeEdgeCount { get { return _freeEdgeCount; } }
        public int FaceCount { get { return _faceCount; } }
        public bool UsedFallback { get { return _usedFallback; } }
        public string Warning { get { return _warning; } }
        public string FaceWarning { get { return _faceWarning; } }


        // Constructors
        public QuadWildEdgeRecoveryInfo(bool success, int edgeCount, int segmentCount, bool usedFallback,
                                        string warning)
            : this(success, edgeCount, segmentCount, 0, 0, usedFallback, warning, null)
        {
        }
        public QuadWildEdgeRecoveryInfo(bool success, int edgeCount, int segmentCount, int freeEdgeCount,
                                        int faceCount, bool usedFallback, string warning, string faceWarning)
        {
            _success = success;
            _edgeCount = edgeCount;
            _segmentCount = segmentCount;
            _freeEdgeCount = freeEdgeCount;
            _faceCount = faceCount;
            _usedFallback = usedFallback;
            _warning = warning;
            _faceWarning = faceWarning;
        }
    }
}
