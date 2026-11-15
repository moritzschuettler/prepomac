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
    [PmxType(6207)]
    public class PythonPanelState
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _scriptText;
        [PmxKey(1)] private string _fileName;
        [PmxKey(2)] private bool _modified;
        [PmxKey(3)] private double _splitterRatio;


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
