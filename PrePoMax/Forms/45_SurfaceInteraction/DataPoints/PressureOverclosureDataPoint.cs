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
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    [PmxType(10399)]
    public class PressureOverclosureDataPoint
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _pressure;
        [PmxKey(1)] private double _overclosure;


        // Properties                                                                                                               
        [DisplayName("Overclosure\n[?]")]
        [TypeConverter(typeof(CaeGlobals.StringLengthFromConverter))]
        public double Overclosure { get { return _overclosure; } set { _overclosure = value; } }
        //
        [DisplayName("Pressure\n[?]")]
        [TypeConverter(typeof(CaeGlobals.StringPressureFromConverter))]
        public double Pressure { get { return _pressure; } set { _pressure = value; } }


        // Constructors                                                                                                             
        public PressureOverclosureDataPoint()
        {
            _pressure = 0;
            _overclosure = 0;
        }
        public PressureOverclosureDataPoint(double pressure, double overclosure)
        {
            _pressure = pressure;
            _overclosure = overclosure;
        }
    }

}


