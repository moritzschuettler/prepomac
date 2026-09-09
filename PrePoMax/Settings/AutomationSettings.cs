// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
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
        public const int DefaultPort = 8756;
        public const int MinPort = 1024;
        public const int MaxPort = 65535;
        // How far above the wanted port the server looks when it is taken. A predictable small range,
        // so that the address stays something the user can write into a client configuration
        public const int PortProbeCount = 16;
        private bool _startWithApplication;
        private bool _isReadOnly;
        private bool _echoToPythonPanel;
        private int _requestTimeout;
        private int _port;
        private string _accessToken;


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
        public int Port
        {
            get { return _port; }
            set { _port = Math.Min(MaxPort, Math.Max(MinPort, value)); }
        }
        // What a client sends to prove it is the client the user configured. It is empty until the
        // endpoint first needs one, and it is written into the session file so that a client which can
        // read that file needs no configuration at all
        public string AccessToken
        {
            get { return _accessToken == null ? "" : _accessToken; }
            set { _accessToken = value == null ? "" : value.Trim(); }
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
        // The port and the token were added after the settings file format was already in use, and
        // deserialization does not run the constructor, so a file written by an older version leaves
        // the port at zero rather than at its default
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_port < MinPort || _port > MaxPort) _port = DefaultPort;
            if (_accessToken == null) _accessToken = "";
        }
        public void Reset()
        {
            _startWithApplication = false;
            _isReadOnly = false;
            _echoToPythonPanel = true;
            _requestTimeout = 30;
            _port = DefaultPort;
            _accessToken = "";
        }
    }
}
