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
    public class SidePanelsState
    {
        // Variables                                                                                                                
        public const int CurrentVersion = 1;
        private int _version;
        private SidePanelTypeEnum _visiblePanel;
        private double _pythonPanelWidthRatio;
        private double _xyPlotPanelWidthRatio;
        private double _advisorPanelWidthRatio;
        private PythonPanelState _pythonPanelState;
        private XYPlotPanelState _xyPlotPanelState;
        private AdvisorPanelState _advisorPanelState;


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
