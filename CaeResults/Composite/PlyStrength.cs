// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeResults
{
    // The ply strengths of a material, the compressive ones as positive values
    [Serializable]
    public class PlyStrength
    {
        // Variables
        private double _tensile1;
        private double _compressive1;
        private double _tensile2;
        private double _compressive2;
        private double _shear12;
        private double _shear23;
        private double _tsaiWuCoefficient;


        // Properties
        public double Tensile1 { get { return _tensile1; } }
        public double Compressive1 { get { return _compressive1; } }
        public double Tensile2 { get { return _tensile2; } }
        public double Compressive2 { get { return _compressive2; } }
        public double Shear12 { get { return _shear12; } }
        public double Shear23 { get { return _shear23; } }
        public double TsaiWuCoefficient { get { return _tsaiWuCoefficient; } }


        // Constructor
        public PlyStrength(double tensile1, double compressive1, double tensile2, double compressive2, double shear12,
                           double shear23, double tsaiWuCoefficient)
        {
            _tensile1 = tensile1;
            _compressive1 = compressive1;
            _tensile2 = tensile2;
            _compressive2 = compressive2;
            _shear12 = shear12;
            _shear23 = shear23;
            _tsaiWuCoefficient = tsaiWuCoefficient;
        }
    }
}
