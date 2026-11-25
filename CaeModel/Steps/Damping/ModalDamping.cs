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
    [PmxType(3226)]
    public enum ModalDampingTypeEnum
    {
        Off,
        Constant,
        Direct,
        Rayleigh
    }

    [PmxType(3227)]
    public class ModalDamping : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private ModalDampingTypeEnum _dampingType;
        [PmxKey(1)] private double _alpha;
        [PmxKey(2)] private double _beta;
        [PmxKey(3)] private double _viscousDampingRatio;
        [PmxKey(4)] private List<DampingRatioAndRange> _dampingRatiosAndRanges;


        // Properties                                                                                                               
        public ModalDampingTypeEnum DampingType { get { return _dampingType; } set { _dampingType = value; } }
        public double Alpha { get { return _alpha; } set { _alpha = value; } }
        public double Beta { get { return _beta; } set { _beta = value; } }
        public double ViscousDampingRatio { get { return _viscousDampingRatio; } set { _viscousDampingRatio = value; } }
        public List<DampingRatioAndRange> DampingRatiosAndRanges
        {
            get { return _dampingRatiosAndRanges; }
            set { _dampingRatiosAndRanges = value; }
        }


        // Constructors                                                                                                             
        public ModalDamping()
            : base("ModalDamping")
        {
            _dampingType = ModalDampingTypeEnum.Off;
            _alpha = 0.0;
            _beta = 0.0;
            _viscousDampingRatio = 0.0;
        }


        // Methods                                                                                                                  
    }
}
