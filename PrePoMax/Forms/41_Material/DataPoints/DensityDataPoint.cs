// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public class DensityDataPoint : TempDataPoint
    {
        // Variables                                                                                                                
        private EquationContainer _density;


        // Properties                                                                                                               
        [DisplayName("Density\n[?]")]
        [TypeConverter(typeof(EquationDensityFromConverter))]
        public EquationString DensityEq { get { return _density.Equation; } set { _density.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer Density { get { return _density; } set { _density = value; } }


        // Constructors                                                                                                             
        public DensityDataPoint()
            :base(0)
        {
            _density = new EquationContainer(typeof(StringDensityConverter), 0); // must not use FROM converter
        }
        public DensityDataPoint(EquationContainer density, EquationContainer temperature)
            :base(temperature)
        {
            _density = density;
        }
    }
}


