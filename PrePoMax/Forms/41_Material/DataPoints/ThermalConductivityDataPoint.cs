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
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ThermalConductivityDataPoint : TempDataPoint
    {
        // Variables                                                                                                                
        private EquationContainer _thermalConductivity;


        // Properties                                                                                                               
        [DisplayName("Thermal conductivity\n[?]")]
        [TypeConverter(typeof(EquationThermalConductivityFromConverter))]
        public EquationString ThermalConductivityEq
        {
            get { return _thermalConductivity.Equation; }
            set { _thermalConductivity.Equation = value; }
        }
        //
        [Browsable(false)]
        public EquationContainer ThermalConductivity { get { return _thermalConductivity; } set { _thermalConductivity = value; } }


        // Constructors                                                                                                             
        public ThermalConductivityDataPoint()
            : base(0)
        {
            _thermalConductivity = new EquationContainer(typeof(StringThermalConductivityConverter), 0); // must not use FROM converter
        }
        public ThermalConductivityDataPoint(EquationContainer thermalConductivity, EquationContainer temperature)
            : base(temperature)
        {
            _thermalConductivity = thermalConductivity;
        }


        // Methods                                                                                                                  
    }
}


