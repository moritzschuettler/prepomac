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
    [PmxType(10412)]
    public class AmplitudeDataPoint
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _time;
        [PmxKey(1)] private double _amplitude;


        // Properties                                                                                                               
        [DisplayName("Time [?]\nFrequency [?]")]
        [TypeConverter(typeof(StringDoubleConverter))]
        public double Time { get { return _time; } set { _time = value; } }
        //
        [DisplayName("Amplitude\n[?]")]
        [TypeConverter(typeof(StringDoubleConverter))]
        public double Amplitude { get { return _amplitude; } set { _amplitude = value; } }


        // Constructors                                                                                                             
        public AmplitudeDataPoint()
        {
            _time = 0;
            _amplitude = 0;
        }
        public AmplitudeDataPoint(double time, double amplitude)
        {
            _time = time;
            _amplitude = amplitude;
        }
    }
}
