// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // The automation server. It serves two fronts onto one dispatcher: the MCP endpoint an agent
    // client connects to, on loopback HTTP, and a named pipe that only the user running PrePoMax may
    // open, for a client that speaks the protocol directly. It never touches the model itself - a
    // handler reaches the application through the dispatcher - and neither front owns a handler of
    // its own.
    //
    // Both go up and down together, on the one switch the user has: an endpoint that is open while
    // nobody is looking is the thing this server may not become.
    public class RpcServer
    {
        // Variables                                                                                                                
        private const int MaximumConnections = 4;
        private const int ListenerReleaseTimeout = 1000;
        private readonly FrmMain _form;
        private readonly Controller _controller;
        private readonly RpcDispatcher _dispatcher;
        private readonly RpcUiThread _uiThread;
        private readonly object _connectionLock;
        private readonly object _listenerLock;
        private readonly List<RpcSession> _sessions;
        private readonly Mcp.McpHttpServer _httpServer;
        private string _pipeName;
        private DateTime _startTime;
        private Thread _listener;
        private NamedPipeServerStream _listening;
        private bool _isRunning;
        private volatile bool _isStopping;
        private bool _isReadOnlyRequested;
        private int _requestedPort;
        private string _lastError;


        // Properties                                                                                                               
        public bool IsRunning { get { return _isRunning; } }
        public string PipeName { get { return _pipeName; } }
        public Mcp.McpHttpServer HttpServer { get { return _httpServer; } }
        public string EndpointUrl { get { return _httpServer.EndpointUrl; } }
        public int EndpointPort { get { return _httpServer.Port; } }
        public DateTime StartTime { get { return _startTime; } }
        public string LastError { get { return _lastError; } }
        public Controller Controller { get { return _controller; } }
        public FrmMain Form { get { return _form; } }
        public int ConnectionCount
        {
            get
            {
                lock (_connectionLock) return _sessions.Count;
            }
        }
        // The command line asks for read only for the whole run; the setting is the user's own choice,
        // and either of them is enough
        public bool IsReadOnlyRequested
        {
            get { return _isReadOnlyRequested; }
            set { _isReadOnlyRequested = value; }
        }
        // The port the command line asked for, for this run only. Zero means the settings decide;
        // this is deliberately not written into them, because a switch for one run must not change
        // the address the user configured a client with
        public int RequestedPort
        {
            get { return _requestedPort; }
            set { _requestedPort = value; }
        }
        public bool IsReadOnly { get { return _isReadOnlyRequested || Settings.IsReadOnly; } }
        public AutomationSettings Settings
        {
            get
            {
                if (_controller == null) return new AutomationSettings();
                AutomationSettings settings = _controller.AutomationSettings;
                return settings == null ? new AutomationSettings() : settings;
            }
        }


        // Events                                                                                                                   
        public event Action StateChanged;


        // Constructor                                                                                                              
        public RpcServer(FrmMain form, Controller controller)
        {
            _form = form;
            _controller = controller;
            _uiThread = new RpcUiThread(form);
            _dispatcher = new RpcDispatcher(this);
            _connectionLock = new object();
            _listenerLock = new object();
            _sessions = new List<RpcSession>();
            _httpServer = new Mcp.McpHttpServer(this);
            _pipeName = null;
            _startTime = DateTime.MinValue;
            _listener = null;
            _listening = null;
            _isRunning = false;
            _isStopping = false;
            _isReadOnlyRequested = false;
            _requestedPort = 0;
            _lastError = null;
        }


        // Methods                                                                                                                  
        public void Start()
        {
            if (_isRunning) return;
            // The endpoint is opened first, so that a port which cannot be had leaves nothing running
            // and nothing to stop: the caller is told, and a manual start says so in a dialog
            _httpServer.Start();
            //
            _pipeName = RpcSessionInfoWriter.GetPipeName();
            _startTime = DateTime.Now;
            _isStopping = false;
            _isRunning = true;
            //
            _listener = new Thread(Listen);
            _listener.IsBackground = true;
            _listener.Name = "PrePoMax automation server";
            _listener.Start();
            //
            RpcSessionInfoWriter.Write(_controller == null ? null : _controller.OpenedFileName, _startTime,
                                       _httpServer.EndpointUrl, _httpServer.Port, _httpServer.AccessToken);
            OnStateChanged();
        }
        public void Stop()
        {
            if (!_isRunning) return;
            //
            _isStopping = true;
            _isRunning = false;
            ReleaseListener();
            //
            List<RpcSession> sessions;
            lock (_connectionLock) sessions = new List<RpcSession>(_sessions);
            foreach (RpcSession session in sessions) session.Close();
            //
            _httpServer.Stop();
            RpcSessionInfoWriter.Remove();
            _pipeName = null;
            OnStateChanged();
        }
        // Stopping closes the connection the request arrived on, so it is posted to the user interface
        // thread rather than done here: the response is written first and the indicators are updated
        // where they may be touched
        public void RequestStop()
        {
            if (_form == null || !_form.IsHandleCreated)
            {
                Stop();
                return;
            }
            _form.BeginInvoke(new MethodInvoker(Stop));
        }
        // The listener waits in WaitForConnection, and closing the pipe under it ends that wait by
        // throwing - on every shutdown, and where a debugger stops on it. Opening the pipe ends the wait
        // the way a client would: the listener returns from it, finds the server stopping and closes the
        // instance itself. The handle is closed here only when that connection cannot be made, so that a
        // listener which cannot be reached is not left holding the pipe
        private void ReleaseListener()
        {
            string pipeName = _pipeName;
            NamedPipeServerStream listening;
            lock (_listenerLock)
            {
                listening = _listening;
                _listening = null;
            }
            if (listening == null || pipeName == null) return;
            //
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
                {
                    client.Connect(ListenerReleaseTimeout);
                    return;
                }
            }
            catch { }
            //
            try { listening.Dispose(); }
            catch { }
        }
        public RpcUiThread GetUiThread()
        {
            return _uiThread;
        }
        public RpcDispatcher GetDispatcher()
        {
            return _dispatcher;
        }
        private void Listen()
        {
            int failures = 0;
            while (!_isStopping)
            {
                NamedPipeServerStream stream = null;
                try
                {
                    // A server that is stopping must not open another instance of the pipe: the wait on
                    // it would begin after the stop released the instance it knew about, and nothing
                    // would be left to end it
                    lock (_listenerLock)
                    {
                        if (_isStopping) return;
                        stream = CreateStream();
                        _listening = stream;
                    }
                    stream.WaitForConnection();
                    lock (_listenerLock)
                    {
                        if (_listening == stream) _listening = null;
                    }
                    failures = 0;
                    if (_isStopping)
                    {
                        stream.Dispose();
                        return;
                    }
                    Serve(stream);
                    stream = null;
                }
                catch (Exception exception)
                {
                    // A stop that cannot reach the pipe disposes the stream the wait blocks on, and a
                    // client that disconnects while connecting is not an error either
                    if (stream != null)
                    {
                        lock (_listenerLock)
                        {
                            if (_listening == stream) _listening = null;
                        }
                        try { stream.Dispose(); }
                        catch { }
                    }
                    if (_isStopping) return;
                    failures++;
                    ReportError("The automation server could not accept a connection: " +
                                exception.Message);
                    // A listener that cannot listen is closed rather than left spinning
                    if (failures >= 3)
                    {
                        RequestStop();
                        return;
                    }
                    Thread.Sleep(200);
                }
            }
        }
        private void ReportError(string message)
        {
            _lastError = message;
            Action<string> write = MessageBoxes.WriteDataToOutput;
            if (write != null) write(message);
        }
        private NamedPipeServerStream CreateStream()
        {
            PipeSecurity security = new PipeSecurity();
            SecurityIdentifier user = WindowsIdentity.GetCurrent().User;
            // CreateNewInstance is what lets this process open the next instance of the pipe once the
            // current one is taken; without it the second constructor call is denied by the descriptor
            // this one wrote
            PipeAccessRights rights = PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance;
            security.AddAccessRule(new PipeAccessRule(user, rights, AccessControlType.Allow));
            // Only the interactive user; not the local system account and not another account on the machine.
            // Asynchronous, because Windows serializes every request on a synchronous handle: closing the
            // stream of a client that sits idle between requests would queue behind its pending read and
            // never return, and the read only ends when that client speaks again
            return new NamedPipeServerStream(_pipeName, PipeDirection.InOut, MaximumConnections,
                                             PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096,
                                             security);
        }
        private void Serve(NamedPipeServerStream stream)
        {
            RpcSession session = new RpcSession(this, stream);
            lock (_connectionLock) _sessions.Add(session);
            OnStateChanged();
            //
            Thread thread = new Thread(delegate ()
            {
                try
                {
                    session.Run();
                }
                finally
                {
                    lock (_connectionLock) _sessions.Remove(session);
                    OnStateChanged();
                }
            });
            thread.IsBackground = true;
            thread.Name = "PrePoMax automation session";
            thread.Start();
        }
        private void OnStateChanged()
        {
            Action changed = StateChanged;
            if (changed == null) return;
            //
            try
            {
                changed();
            }
            catch (Exception)
            {
                // A connection thread reports the state, and the last one reports it while the
                // application is already closing. A listener that cannot be told must not take
                // the thread - or the process - down with it
            }
        }
    }
}
