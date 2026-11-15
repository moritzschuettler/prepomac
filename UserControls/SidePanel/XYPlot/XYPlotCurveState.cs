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

namespace UserControls
{
    [Serializable]
    [PmxType(6211)]
    public class XYPlotCurveState
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double[] _xValues;
        [PmxKey(1)] private double[] _yValues;
        [PmxKey(2)] private XYPlotCurveSettings _settings;


        // Properties                                                                                                               
        public double[] XValues { get { return _xValues; } set { _xValues = value ?? new double[0]; } }
        public double[] YValues { get { return _yValues; } set { _yValues = value ?? new double[0]; } }
        public XYPlotCurveSettings Settings
        {
            get { return _settings; }
            set { _settings = value ?? new XYPlotCurveSettings(); }
        }


        // Constructor                                                                                                              
        public XYPlotCurveState()
        {
            _xValues = new double[0];
            _yValues = new double[0];
            _settings = new XYPlotCurveSettings();
        }
    }
}
