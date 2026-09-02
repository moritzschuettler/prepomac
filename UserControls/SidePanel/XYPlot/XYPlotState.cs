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

namespace UserControls
{
    [Serializable]
    public class XYPlotState
    {
        // Variables                                                                                                                
        private XYPlotSettings _settings;
        private List<XYPlotCurveState> _curves;
        private int _selectedCurveIndex;


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
