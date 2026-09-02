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
    public class XYPlotPanelState
    {
        // Variables                                                                                                                
        private string _title;
        private int _columnCount;
        private double _splitterRatio;
        private List<XYPlotState> _plots;
        private int _selectedPlotIndex;


        // Properties                                                                                                               
        public string Title { get { return _title; } set { _title = value ?? ""; } }
        public int ColumnCount { get { return _columnCount; } set { _columnCount = value; } }
        public double SplitterRatio { get { return _splitterRatio; } set { _splitterRatio = value; } }
        public List<XYPlotState> Plots { get { return _plots; } }
        public int SelectedPlotIndex { get { return _selectedPlotIndex; } set { _selectedPlotIndex = value; } }


        // Constructor                                                                                                              
        public XYPlotPanelState()
        {
            _title = "XY Plot";
            _columnCount = 1;
            _splitterRatio = 0.5;
            _plots = new List<XYPlotState>();
            _selectedPlotIndex = 0;
        }
    }
}
