// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax.Rpc
{
    // One running instance, as its own session file describes it. It is a read model: every value in
    // it was written by the instance it names, and the reader trusts none of them further than the
    // process id, which it checks against the process table.
    public class RpcSessionInfo
    {
        // Variables                                                                                                                
        private readonly int _processId;
        private readonly string _pipeName;
        private readonly string _endpointUrl;
        private readonly int _port;
        private readonly string _accessToken;
        private readonly string _protocolVersion;
        private readonly string _applicationVersion;
        private readonly string _modelFileName;
        private readonly string _startTime;


        // Properties                                                                                                               
        public int ProcessId { get { return _processId; } }
        public string PipeName { get { return _pipeName; } }
        public string EndpointUrl { get { return _endpointUrl; } }
        public int Port { get { return _port; } }
        public string AccessToken { get { return _accessToken; } }
        public string ProtocolVersion { get { return _protocolVersion; } }
        public string ApplicationVersion { get { return _applicationVersion; } }
        public string ModelFileName { get { return _modelFileName; } }
        public string StartTime { get { return _startTime; } }


        // Constructor                                                                                                              
        public RpcSessionInfo(int processId, string pipeName, string endpointUrl, int port, string accessToken,
                              string protocolVersion, string applicationVersion, string modelFileName,
                              string startTime)
        {
            _processId = processId;
            _pipeName = pipeName;
            _endpointUrl = endpointUrl;
            _port = port;
            _accessToken = accessToken;
            _protocolVersion = protocolVersion;
            _applicationVersion = applicationVersion;
            _modelFileName = modelFileName;
            _startTime = startTime;
        }


        // Methods                                                                                                                  
        public override string ToString()
        {
            string model = string.IsNullOrEmpty(_modelFileName) ? "no file" : _modelFileName;
            string address = string.IsNullOrEmpty(_endpointUrl) ? "no endpoint" : _endpointUrl;
            return "PrePoMax " + _applicationVersion + ", process " + _processId + ", " + model + ", " + address;
        }
    }
}
