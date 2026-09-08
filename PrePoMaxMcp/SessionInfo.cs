// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMaxMcp
{
    // One running PrePoMax instance, as its own discovery file describes it.
    public class SessionInfo
    {
        // Variables                                                                                                                
        private readonly int _processId;
        private readonly string _pipeName;
        private readonly string _protocolVersion;
        private readonly string _applicationVersion;
        private readonly string _modelFileName;
        private readonly string _startTime;


        // Properties                                                                                                               
        public int ProcessId { get { return _processId; } }
        public string PipeName { get { return _pipeName; } }
        public string ProtocolVersion { get { return _protocolVersion; } }
        public string ApplicationVersion { get { return _applicationVersion; } }
        public string ModelFileName { get { return _modelFileName; } }
        public string StartTime { get { return _startTime; } }


        // Constructor                                                                                                              
        public SessionInfo(int processId, string pipeName, string protocolVersion, string applicationVersion,
                           string modelFileName, string startTime)
        {
            _processId = processId;
            _pipeName = pipeName;
            _protocolVersion = protocolVersion;
            _applicationVersion = applicationVersion;
            _modelFileName = modelFileName;
            _startTime = startTime;
        }


        // Methods                                                                                                                  
        public override string ToString()
        {
            string model = string.IsNullOrEmpty(_modelFileName) ? "no file" : _modelFileName;
            return "PrePoMax " + _applicationVersion + ", process " + _processId + ", " + model;
        }
    }
}
