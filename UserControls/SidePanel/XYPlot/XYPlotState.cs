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
using CaeGlobals;

namespace UserControls
{
    [PmxType(6217)]
    public class XYPlotState
    {
        // Variables                                                                                                                
        [PmxKey(0)] private XYPlotSettings _settings;
        [PmxKey(1)] private List<XYPlotCurveState> _curves;
        [PmxKey(2)] private int _selectedCurveIndex;


        // Properties                                                                                                               
        public XYPlotSettings Settings
        {
            get { return _settings; }
            set { _settings = value ?? new XYPlotSettings(); }
        }
        public List<XYPlotCurveState> Curves { get { return _curves; } }
        public int SelectedCurveIndex { get { return _selectedCurveIndex; } set { _selectedCurveIndex = value; } }


        // Constructor                                                                                                              
        public XYPlotState()
        {
            _settings = new XYPlotSettings();
            _curves = new List<XYPlotCurveState>();
            _selectedCurveIndex = -1;
        }
    }
}
