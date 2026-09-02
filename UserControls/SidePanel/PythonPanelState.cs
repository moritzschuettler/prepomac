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
    public class PythonPanelState
    {
        // Variables                                                                                                                
        private string _scriptText;
        private string _fileName;
        private bool _modified;
        private double _splitterRatio;


        // Properties                                                                                                               
        public string ScriptText { get { return _scriptText; } set { _scriptText = value ?? ""; } }
        public string FileName { get { return _fileName; } set { _fileName = value ?? ""; } }
        public bool Modified { get { return _modified; } set { _modified = value; } }
        public double SplitterRatio { get { return _splitterRatio; } set { _splitterRatio = value; } }


        // Constructor                                                                                                              
        public PythonPanelState()
        {
            _scriptText = "";
            _fileName = "";
            _modified = false;
            _splitterRatio = 0.75;
        }
    }
}
