// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// One geometry-validated translational move in a disassembly plan. Several clusters identify a
    /// temporary subassembly that has to leave the remaining assembly together.
    /// </summary>
    public class DisassemblyMove
    {
        private int[] _clusterIds;
        private Vec3D _direction;
        private double _distance;
        private int _step;
        private double _confidence;
        private bool _temporarySubassembly;

        public int[] ClusterIds { get { return _clusterIds; } }
        public Vec3D Direction { get { return _direction; } }
        public double Distance { get { return _distance; } }
        public int Step { get { return _step; } internal set { _step = value; } }
        public double Confidence { get { return _confidence; } }
        public bool TemporarySubassembly { get { return _temporarySubassembly; } }

        public DisassemblyMove(int[] clusterIds, Vec3D direction, double distance, int step,
                               double confidence, bool temporarySubassembly)
        {
            _clusterIds = (int[])clusterIds.Clone();
            _direction = new Vec3D(direction);
            _distance = distance;
            _step = step;
            _confidence = confidence;
            _temporarySubassembly = temporarySubassembly;
        }
    }
}
