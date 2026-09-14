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
using System.Net.Sockets;
using System.Text;
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
        private const int ListenerReleaseTimeout = 1000;
        private readonly RpcServer _server;
        private HttpListener _listener;
        private Thread _listenerThread;
        private string _accessToken;
        private int _port;
        private bool _isRunning;
        private volatile bool _isStopping;
        private string _lastError;


        // Properties                                                                                                               
        public RpcServer Server { get { return _server; } }
        public bool IsRunning { get { return _isRunning; } }
        public int Port { get { return _port; } }
        public string AccessToken { get { return _accessToken == null ? "" : _accessToken; } }
        public string LastError { get { return _lastError; } }
        public string EndpointUrl { get { return _isRunning ? GetEndpointUrl(_port) : ""; } }


        // Constructor                                                                                                              
        public McpHttpServer(RpcServer server)
        {
            _server = server;
            _listener = null;
            _listenerThread = null;
            _accessToken = null;
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
            _accessToken = _server.Settings.AccessToken;
            if (_accessToken.Length == 0)
            {
                throw new CaeException("The automation server has no access token, so a client could not be told " +
                                       "how to reach it. Generate one in Tools - Settings - Automation, in the " +
                                       "context menu of the Access token entry.");
            }
            int wanted = _server.RequestedPort > 0 ? _server.RequestedPort : _server.Settings.Port;
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
            HttpListener listener = _listener;
            ReleaseListener(_port);
            _listener = null;
            Close(listener);
            _port = 0;
        }
        // The listener thread waits in GetContext, and closing the listener under it ends that wait by
        // throwing - on every shutdown, and where a debugger stops on it. A request of the server's own
        // ends the wait the way a client would: the thread returns from it, finds the endpoint stopping
        // and leaves the loop, so the listener is closed here with nothing left waiting on it. It is
        // written to a socket rather than sent through HttpWebRequest because no answer is wanted and
        // none is read - the request is only the thing that makes GetContext return. Waiting for the
        // thread cannot hold up the user interface: it hands every request to a pool thread and waits
        // again, so it is never the thread an answer is being written on
        private void ReleaseListener(int port)
        {
            Thread thread = _listenerThread;
            _listenerThread = null;
            if (thread == null || port == 0) return;
            //
            try
            {
                string host = "127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);
                byte[] request = Encoding.ASCII.GetBytes("GET " + EndpointPath + " HTTP/1.1\r\nHost: " + host +
                                                         "\r\nConnection: close\r\n\r\n");
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);
                    client.GetStream().Write(request, 0, request.Length);
                }
            }
            catch { }
            //
            thread.Join(ListenerReleaseTimeout);
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
                    // A stop the request of its own did not reach closes the listener underneath the wait
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
        private static void Close(HttpListener listener)
        {
            if (listener == null) return;
            //
            try { listener.Close(); }
            catch { }
        }
    }
}
