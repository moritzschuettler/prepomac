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
        // How long a caller is waited for: for its turn, for the window, and for an exec or an eval
        // before it is answered that the call is still running
        public const int MinRequestTimeout = 5;
        public const int MaxRequestTimeout = 600;
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
            set { _requestTimeout = Math.Min(MaxRequestTimeout, Math.Max(MinRequestTimeout, value)); }
        }
        public int Port
        {
            get { return _port; }
            set { _port = Math.Min(MaxPort, Math.Max(MinPort, value)); }
        }
        // The access token of the endpoint. It lives here, beside the executable, so that deleting the
        // portable folder deletes it and the user can read it back to configure a client.
        //
        // The settings file is part of the redistributable, so a token left in it would be shipped to
        // every user, identical for all of them - which is why it is cleared on the first run in a
        // folder, and why Tools - Settings - Automation offers an action to clear it before a release
        // is prepared
        public string AccessToken
        {
            get { return _accessToken == null ? "" : _accessToken; }
            set { _accessToken = value == null ? "" : value.Trim(); }
        }


        // Constructors                                                                                                             
        public AutomationSettings()
        {
            Reset();
            _accessToken = "";
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
        // The access token is not a setting but the result of an action that asks first, and a running
        // server keeps serving the token it started with, so resetting the settings keeps it
        public void Reset()
        {
            _startWithApplication = false;
            _isReadOnly = false;
            _echoToPythonPanel = true;
            _requestTimeout = 30;
            _port = DefaultPort;
        }
    }
}
