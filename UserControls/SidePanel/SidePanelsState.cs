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
    [PmxType(6208)]
    public class SidePanelsState
    {
        // Variables                                                                                                                
        public const int CurrentVersion = 1;
        [PmxKey(0)] private int _version;
        [PmxKey(1)] private SidePanelTypeEnum _visiblePanel;
        [PmxKey(2)] private double _pythonPanelWidthRatio;
        [PmxKey(3)] private double _xyPlotPanelWidthRatio;
        [PmxKey(4)] private double _advisorPanelWidthRatio;
        [PmxKey(5)] private PythonPanelState _pythonPanelState;
        [PmxKey(6)] private XYPlotPanelState _xyPlotPanelState;
        [PmxKey(7)] private AdvisorPanelState _advisorPanelState;


        // Properties                                                                                                               
        public int Version { get { return _version; } }
        public SidePanelTypeEnum VisiblePanel { get { return _visiblePanel; } set { _visiblePanel = value; } }
        public double PythonPanelWidthRatio
        {
            get { return _pythonPanelWidthRatio; }
            set { _pythonPanelWidthRatio = value; }
        }
        public double XYPlotPanelWidthRatio
        {
            get { return _xyPlotPanelWidthRatio; }
            set { _xyPlotPanelWidthRatio = value; }
        }
        public double AdvisorPanelWidthRatio
        {
            get { return _advisorPanelWidthRatio; }
            set { _advisorPanelWidthRatio = value; }
        }
        public PythonPanelState PythonPanelState
        {
            get { return _pythonPanelState; }
            set { _pythonPanelState = value; }
        }
        public XYPlotPanelState XYPlotPanelState
        {
            get { return _xyPlotPanelState; }
            set { _xyPlotPanelState = value; }
        }
        public AdvisorPanelState AdvisorPanelState
        {
            get { return _advisorPanelState; }
            set { _advisorPanelState = value; }
        }


        // Constructor                                                                                                              
        public SidePanelsState()
        {
            _version = CurrentVersion;
            _visiblePanel = SidePanelTypeEnum.None;
            _pythonPanelWidthRatio = 0.5;
            _xyPlotPanelWidthRatio = 0.5;
            _advisorPanelWidthRatio = 0.3;
            _pythonPanelState = null;
            _xyPlotPanelState = null;
            _advisorPanelState = null;
        }
    }
}
