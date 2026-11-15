// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace CaeResults
{
    // The in-plane elastic constants of a ply, which give its plane stresses from its in-plane strains
    [Serializable]
    [PmxType(5000)]
    public class PlyStiffness
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _q11;
        [PmxKey(1)] private double _q22;
        [PmxKey(2)] private double _q12;
        [PmxKey(3)] private double _q66;


        // Constructor                                                                                                              
        public PlyStiffness(double youngsModulus1, double youngsModulus2, double poissonsRatio12, double shearModulus12)
        {
            double poissonsRatio21 = poissonsRatio12 * youngsModulus2 / youngsModulus1;
            double denominator = 1 - poissonsRatio12 * poissonsRatio21;
            _q11 = youngsModulus1 / denominator;
            _q22 = youngsModulus2 / denominator;
            _q12 = poissonsRatio12 * youngsModulus2 / denominator;
            _q66 = shearModulus12;
        }


        // Methods                                                                                                                  
        // The strains are tensor components, so the engineering shear strain is twice e12
        public void GetPlaneStress(double e11, double e22, double e12, out double s11, out double s22, out double s12)
        {
            s11 = _q11 * e11 + _q12 * e22;
            s22 = _q12 * e11 + _q22 * e22;
            s12 = _q66 * 2 * e12;
        }
    }
}
