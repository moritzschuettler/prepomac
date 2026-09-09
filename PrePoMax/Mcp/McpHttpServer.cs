// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Rpc;
using System;
using System.Globalization;
using System.Net;
using System.Threading;

namespace PrePoMax.Mcp
{
    // The MCP endpoint. It listens on loopback only, on the port the user chose or the first free one
    // above it, and hands every accepted request to an McpHttpRequestHandler on a thread of its own.
    // It holds no model state: a handler reaches the application through the same RpcDispatcher the
    // named pipe reaches it through, so the two fronts share one method table and neither owns a
    // handler.
    //
    // Both loopback prefixes are registered because http.sys routes by the Host header of the
    // request: a client configured with localhost would otherwise be refused by a listener that had
    // only claimed 127.0.0.1. The wildcard prefix is never registered - it needs an elevated URL
    // reservation, which a portable application has no install step to make, and it would put the
    // endpoint on the network.
    public class McpHttpServer
    {
        // Variables                                                                                                                
        public const string EndpointPath = "/mcp";
        private const int AccessDenied = 5;
        private readonly RpcServer _server;
        private HttpListener _listener;
        private Thread _listenerThread;
        private string _accessToken;
        private bool _isAccessTokenEphemeral;
        private int _port;
        private bool _isRunning;
        private bool _isStopping;
        private string _lastError;


        // Properties                                                                                                               
        public RpcServer Server { get { return _server; } }
        public bool IsRunning { get { return _isRunning; } }
        public int Port { get { return _port; } }
        public string AccessToken { get { return _accessToken == null ? "" : _accessToken; } }
        public bool IsAccessTokenEphemeral { get { return _isAccessTokenEphemeral; } }
        public string LastError { get { return _lastError; } }
        public string EndpointUrl { get { return _isRunning ? GetEndpointUrl(_port) : ""; } }


        // Constructor                                                                                                              
        public McpHttpServer(RpcServer server)
        {
            _server = server;
            _listener = null;
            _listenerThread = null;
            _accessToken = null;
            _isAccessTokenEphemeral = false;
            _port = 0;
            _isRunning = false;
            _isStopping = false;
            _lastError = null;
        }


        // Static methods                                                                                                           
        public static string GetEndpointUrl(int port)
        {
            return "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture) + EndpointPath;
        }


        // Methods                                                                                                                  
        public void Start()
        {
            if (_isRunning) return;
            //
            ResolveAccessToken();
            int wanted = _server.Settings.Port;
            if (wanted < AutomationSettings.MinPort || wanted > AutomationSettings.MaxPort)
                wanted = AutomationSettings.DefaultPort;
            int last = wanted;
            _isStopping = false;
            for (int offset = 0; offset < AutomationSettings.PortProbeCount; offset++)
            {
                int port = wanted + offset;
                if (port > AutomationSettings.MaxPort) break;
                last = port;
                //
                HttpListener listener = CreateListener(port);
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException exception)
                {
                    Close(listener);
                    // Access denied means the prefix itself is refused, which the next port will not
                    // cure; any other code means this port is taken
                    if (exception.ErrorCode == AccessDenied)
                    {
                        throw new CaeException("The MCP endpoint may not be opened on " + GetEndpointUrl(port) +
                                               ": " + exception.Message);
                    }
                    continue;
                }
                _listener = listener;
                _port = port;
                break;
            }
            if (_listener == null)
            {
                throw new CaeException("The MCP endpoint could not be opened: every port from " + wanted + " to " +
                                       last + " is taken. Choose another one in Tools - Settings - Automation.");
            }
            _isRunning = true;
            _listenerThread = new Thread(Listen);
            _listenerThread.IsBackground = true;
            _listenerThread.Name = "PrePoMax MCP endpoint";
            _listenerThread.Start();
        }
        public void Stop()
        {
            if (!_isRunning) return;
            //
            _isStopping = true;
            _isRunning = false;
            // Closing the listener is what ends the wait the listener thread sits in
            HttpListener listener = _listener;
            _listener = null;
            Close(listener);
            _port = 0;
        }
        // Where a request becomes an answer. This is the one seam between the transport and the
        // protocol, and null means the request was a notification and there is nothing to send back
        public string HandleProtocol(string requestBody)
        {
            return McpProtocolServer.Handle(requestBody, _server.GetDispatcher());
        }
        public void ReportError(string message)
        {
            _lastError = message;
            Action<string> write = MessageBoxes.WriteDataToOutput;
            if (write != null) write(message);
        }
        private void Listen()
        {
            int failures = 0;
            while (!_isStopping)
            {
                HttpListener listener = _listener;
                if (listener == null) return;
                //
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    failures = 0;
                    if (_isStopping) return;
                    Serve(context);
                }
                catch (HttpListenerException)
                {
                    // The listener was closed underneath the wait, which is how Stop ends it
                    if (_isStopping) return;
                    failures++;
                    ReportError("The MCP endpoint could not accept a request.");
                    if (failures >= 3) return;
                    Thread.Sleep(200);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        }
        // Each request is served on a pool thread, which is the kind of thread the application
        // already runs scripted API calls on, so a call that only reports still answers while a call
        // that works is in flight. What may not run at once is enforced by the dispatcher, not here
        private void Serve(HttpListenerContext context)
        {
            McpHttpRequestHandler handler = new McpHttpRequestHandler(this, context);
            ThreadPool.QueueUserWorkItem(delegate (object state) { handler.Serve(); });
        }
        private HttpListener CreateListener(int port)
        {
            string suffix = ":" + port.ToString(CultureInfo.InvariantCulture) + "/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1" + suffix);
            listener.Prefixes.Add("http://localhost" + suffix);
            return listener;
        }
        // The token the user already has, or a new one. A run that may not write the settings - which
        // is what --noSettingsSave does, and what --script turns on by default - keeps its token for
        // the run only, and the session file is where such a run's client reads it
        private void ResolveAccessToken()
        {
            string token = _server.Settings.AccessToken;
            if (token.Length > 0)
            {
                _accessToken = token;
                _isAccessTokenEphemeral = false;
                return;
            }
            _accessToken = McpAccessToken.Generate();
            Controller controller = _server.Controller;
            if (controller != null && !SettingsContainer.IsSaveDisabled)
            {
                controller.SaveAutomationAccessToken(_accessToken);
                _isAccessTokenEphemeral = false;
            }
            else _isAccessTokenEphemeral = true;
        }
        private static void Close(HttpListener listener)
        {
            if (listener == null) return;
            //
            try { listener.Close(); }
            catch { }
        }
    }
}
