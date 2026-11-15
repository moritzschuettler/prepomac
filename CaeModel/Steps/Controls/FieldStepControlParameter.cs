// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using CaeJob;

namespace CaeModel
{
    [Serializable]
    [PmxType(3217)]
    public class FieldStepControlParameter : StepControlParameter
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _rna;
        [PmxKey(1)] private double _cna;
        [PmxKey(2)] private double _q0a;
        [PmxKey(3)] private double _qua;
        [PmxKey(4)] private double _rpa;
        [PmxKey(5)] private double _ea;
        [PmxKey(6)] private double _cea;
        [PmxKey(7)] private double _rla;


        // Properties                                                                                                               
        public double Rna { get { return _rna; } set { _rna = Math.Max(0, value); } }
        public double Cna { get { return _cna; } set { _cna = Math.Max(0, value); } }
        public double Q0a { get { return _q0a; } set { _q0a = Math.Max(0, value); } }
        public double Qua { get { return _qua; } set { _qua = Math.Max(0, value); } }
        public double Rpa { get { return _rpa; } set { _rpa = Math.Max(0, value); } }
        public double Ea { get { return _ea; } set { _ea = Math.Max(0, value); } }
        public double Cea { get { return _cea; } set { _cea = Math.Max(0, value); } }
        public double Rla { get { return _rla; } set { _rla = Math.Max(0, value); } }


        // Constructors                                                                                                             
        public FieldStepControlParameter()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            _rna = 0.005;
            _cna = 0.01;
            _q0a = double.NaN;
            _qua = double.NaN;
            _rpa = 0.02;
            _ea = 1E-5;
            _cea = 1E-3;
            _rla = 1E-8;
        }
    }
}

