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
    public class SpecificHeatDataPoint : TempDataPoint
    {
        // Variables                                                                                                                
        private EquationContainer _specificHeat;


        // Properties                                                                                                               
        [DisplayName("Specific heat\n[?]")]
        [TypeConverter(typeof(EquationSpecificHeatFromConverter))]
        public EquationString SpecificHeatEq { get { return _specificHeat.Equation; } set { _specificHeat.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer SpecificHeat { get { return _specificHeat; } set { _specificHeat = value; } }



        // Constructors                                                                                                             
        public SpecificHeatDataPoint()
            :base(0)
        {
            _specificHeat = new EquationContainer(typeof(StringSpecificHeatConverter), 0); // must not use FROM converter
        }
        public SpecificHeatDataPoint(EquationContainer density, EquationContainer temperature)
            :base(temperature)
        {
            _specificHeat = density;
        }


        // Methods                                                                                                                  
        
    }
}


