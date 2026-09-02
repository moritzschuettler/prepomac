// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace UserControls
{
    [Serializable]
    public class XYPlotCurveState
    {
        // Variables                                                                                                                
        private double[] _xValues;
        private double[] _yValues;
        private XYPlotCurveSettings _settings;


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
