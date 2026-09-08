// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace PrePoMax
{
    // The automation server is an open door onto the model the user has open, so nothing here starts
    // it unless the user asked for it. Whether it currently runs is session state and lives on the
    // server; these are the choices worth remembering between sessions.
    [Serializable]
    public class AutomationSettings : ISettings
    {
        // Variables                                                                                                                
        private bool _startWithApplication;
        private bool _isReadOnly;
        private bool _echoToPythonPanel;
        private int _requestTimeout;


        // Properties                                                                                                               
        public bool StartWithApplication
        {
            get { return _startWithApplication; }
            set { _startWithApplication = value; }
        }
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }
        public bool EchoToPythonPanel
        {
            get { return _echoToPythonPanel; }
            set { _echoToPythonPanel = value; }
        }
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = Math.Min(600, Math.Max(5, value)); }
        }


        // Constructors                                                                                                             
        public AutomationSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _startWithApplication = false;
            _isReadOnly = false;
            _echoToPythonPanel = true;
            _requestTimeout = 30;
        }
    }
}
