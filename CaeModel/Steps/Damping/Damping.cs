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
    [PmxType(3223)]
    public enum DampingTypeEnum
    {
        Off,
        Rayleigh
    }

    [Serializable]
    [PmxType(3224)]
    public class Damping : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private DampingTypeEnum _dampingType;
        [PmxKey(1)] private double _alpha;
        [PmxKey(2)] private double _beta;


        // Properties                                                                                                               
        public DampingTypeEnum DampingType { get { return _dampingType; } set { _dampingType = value; } }
        public double Alpha { get { return _alpha; } set { _alpha = value; } }
        public double Beta { get { return _beta; } set { _beta = value; } }


        // Constructors                                                                                                             
        public Damping()
            : base("Damping")
        {
            _dampingType = DampingTypeEnum.Off;
            _alpha = 0.0;
            _beta = 0.0;
        }


        // Methods                                                                                                                  
    }
}
