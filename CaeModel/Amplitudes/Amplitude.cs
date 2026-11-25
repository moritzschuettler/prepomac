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
using DynamicTypeDescriptor;

namespace CaeModel
{
    [PmxType(3000)]
    public enum AmplitudeTimeSpanEnum
    {
        [StandardValue("StepTime", DisplayName = "Step time")]
        StepTime,
        [StandardValue("TotalTime", DisplayName = "Total time")]
        TotalTime
    }
    [PmxType(3001)]
    public abstract class Amplitude : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private AmplitudeTimeSpanEnum _timeSpan;
        [PmxKey(1)] private double _shiftX;
        [PmxKey(2)] private double _shiftY;
        [PmxKey(3)] protected double[][] _timeAmplitude;
        public const string DefaultAmplitudeName = "Default";


        // Properties                                                                                                               
        public AmplitudeTimeSpanEnum TimeSpan { get { return _timeSpan; } set { _timeSpan = value; } }
        public double ShiftX { get { return _shiftX; } set { _shiftX = value; } }
        public double ShiftY { get { return _shiftY; } set { _shiftY = value; } }
        public double[][] TimeAmplitude { get { return _timeAmplitude; } set { _timeAmplitude = value; } }

        // Constructors                                                                                                             
        public Amplitude(string name)
            : base(name)
        {
            _timeSpan = AmplitudeTimeSpanEnum.StepTime;
            _shiftX = 0;
            _shiftY = 0;
        }


        // Methods                                                                                                                  
    }
}


