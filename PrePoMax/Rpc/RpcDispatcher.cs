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
using System.Globalization;
using System.Threading;

namespace PrePoMax.Rpc
{
    // The method table. Requests are served one at a time, whichever connection they arrive on, so
    // two agents - or an agent and a script - can never interleave halfway through changing a model.
    // A handler renders its result as a JSON value and the envelope is composed around it; anything
    // it throws that is not an RpcException is an internal error, because it was not foreseen.
    //
    // A call that runs Python can take as long as the work it was asked for, so it runs on a thread of
    // its own and its caller is waited for no longer than the request timeout, or the timeout the call
    // names. A call still running then is answered with its call id rather than abandoned: it goes on
    // holding the turn, its commands are recorded as usual, and its answer is kept until wait collects
    // it. The turn is a semaphore rather than a lock for that reason - it is given back on the thread
    // that ran the call, not on the one that took it.
    public class RpcDispatcher
    {
        // Variables                                                                                                                
        public const int MaximumWaitSeconds = AutomationSettings.MaxRequestTimeout;
        private const int MaximumUncollectedCalls = 16;
        private const int MaximumUncollectedMinutes = 60;
        private readonly RpcServer _server;
        private readonly SemaphoreSlim _turn;
        private readonly object _callLock;
        private readonly Dictionary<int, RpcRunningCall> _calls;
        private readonly object _blockLock;
        private RpcRunningCall _runningCall;
        private int _lastCallId;
        private int _blockCount;
        [ThreadStatic] private static string _echoNote;
        [ThreadStatic] private static List<RpcRunningCall> _undeliveredCalls;


        // Properties                                                                                                               
        public RpcServer Server { get { return _server; } }
        // What the Python panel shows in place of the source of the call this thread runs, or null. Only
        // an in process caller can set it, for a script of its own, so nothing an agent sends is hidden
        public static string CurrentEchoNote { get { return _echoNote; } }


        // Constructor                                                                                                              
        public RpcDispatcher(RpcServer server)
        {
            _server = server;
            _turn = new SemaphoreSlim(1, 1);
            _callLock = new object();
            _calls = new Dictionary<int, RpcRunningCall>();
            _blockLock = new object();
            _runningCall = null;
            _lastCallId = 0;
            _blockCount = 0;
        }


        // Methods                                                                                                                  
        public RpcResponse Dispatch(RpcRequest request)
        {
            try
            {
                return RpcResponse.FromResult(request.Id, Invoke(request));
            }
            catch (RpcException exception)
            {
                return RpcResponse.FromError(request.Id, exception.Code, exception.Message);
            }
            catch (PromptRefusedException exception)
            {
                return RpcResponse.FromError(request.Id, RpcErrorCode.PromptRefused, exception.Message);
            }
            catch (APIs.Python.PythonAPIException exception)
            {
                // The API refused the arguments, and its message is written for a human to read
                return RpcResponse.FromError(request.Id, RpcErrorCode.ApiError, exception.Message);
            }
            catch (Exception exception)
            {
                return RpcResponse.FromError(request.Id, RpcErrorCode.InternalError, exception.Message);
            }
        }
        // The method table without the envelope, for a caller that is already inside the process and
        // has no line to parse: it returns the result as the JSON value a handler wrote and lets the
        // refusals out, so the two fronts share one table and neither owns a handler of its own
        public string Invoke(RpcRequest request)
        {
            // Nothing an agent runs may open a dialog. A handler runs on this connection thread while the
            // window stays the user's, so a dialog is shown on the window and the call waits for a human at
            // the keyboard - and it waits holding the turn below, which is how one call that could not
            // finish stopped every later one
            bool wasNonInteractive = MessageBoxes.IsNonInteractiveThread;
            MessageBoxes.IsNonInteractiveThread = true;
            try
            {
                // A method that only reports is answered while another one is still working, so a
                // client can always ask what is going on; anything that reaches the model waits its
                // turn, whichever connection it arrived on
                if (!IsSerialized(request.Method)) return Execute(request);
                //
                if (ShowsBusyState(request.Method)) return StartWhenFree(request);
                return ExecuteWhenFree(request);
            }
            finally
            {
                MessageBoxes.IsNonInteractiveThread = wasNonInteractive;
            }
        }
        // The turn is waited for with a bound, because an unbounded wait turns one call that cannot finish
        // into a server that never answers again, and a caller cannot tell that from a slow model
        private string ExecuteWhenFree(RpcRequest request)
        {
            WaitForTurn();
            try
            {
                return Execute(request);
            }
            finally
            {
                _turn.Release();
            }
        }
        private void WaitForTurn()
        {
            int seconds = Math.Max(1, _server.RequestTimeout);
            if (_turn.Wait(1000 * seconds)) return;
            //
            RpcRunningCall running = GetRunningCall();
            string waited = " and this one waited " + seconds.ToString(CultureInfo.InvariantCulture) + " s for its " +
                            "turn. Nothing was done; ";
            if (running == null)
                throw new RpcException(RpcErrorCode.Busy, "Another automation call is still running" + waited +
                                                          "call get_status (status on the pipe) and try again.");
            throw new RpcException(RpcErrorCode.Busy, "Automation call " +
                                   running.Id.ToString(CultureInfo.InvariantCulture) + " (" + running.Method +
                                   ") is still running after " + RpcRunningCall.FormatSeconds(running.Elapsed) +
                                   waited + "collect its answer with get_call (wait on the pipe), " +
                                   "then try again.");
        }
        // The call is started once it has the turn, and the turn is given back by the thread that ran it.
        // The caller waits for it as long as it said it would; a call that outlasts that is answered with
        // its id, and goes on
        private string StartWhenFree(RpcRequest request)
        {
            int waitSeconds = GetWaitSeconds(request, "timeout", _server.RequestTimeout);
            WaitForTurn();
            RpcRunningCall call;
            lock (_callLock)
            {
                _lastCallId++;
                call = new RpcRunningCall(_lastCallId, request.Method);
                _runningCall = call;
                _calls.Add(call.Id, call);
                RemoveOldestCollectableCalls();
            }
            try
            {
                call.Start(delegate () { return ExecuteOnCallThread(request); },
                           delegate () { FinishCall(call); });
            }
            catch (Exception)
            {
                lock (_callLock)
                {
                    _calls.Remove(call.Id);
                    if (_runningCall == call) _runningCall = null;
                }
                _turn.Release();
                throw;
            }
            return Collect(call, waitSeconds);
        }
        private string ExecuteOnCallThread(RpcRequest request)
        {
            MessageBoxes.IsNonInteractiveThread = true;
            _echoNote = request.EchoNote;
            try
            {
                return Execute(request);
            }
            finally
            {
                _echoNote = null;
            }
        }
        private void FinishCall(RpcRunningCall call)
        {
            lock (_callLock)
            {
                if (_runningCall == call) _runningCall = null;
            }
            _turn.Release();
        }
        // A finished answer is given up only once it has reached the caller. Inside a delivery the transport
        // has begun, it stays collectable until the transport says the response was written, because a client
        // whose own request timeout ran out first is gone, and its answer would otherwise be lost with it
        private string Collect(RpcRunningCall call, int waitSeconds)
        {
            if (!call.Wait(1000 * waitSeconds)) return call.GetRunningAnswer();
            //
            if (_undeliveredCalls != null) _undeliveredCalls.Add(call);
            else lock (_callLock) _calls.Remove(call.Id);
            return call.GetResult();
        }
        // Begun by a transport on the thread that serves one request, before it is dispatched, and ended once
        // the response was written or failed to be. The answers handed over in between are given up only by a
        // delivery that succeeded; the others stay collectable with get_call
        public void BeginDelivery()
        {
            _undeliveredCalls = new List<RpcRunningCall>();
        }
        public void EndDelivery(bool isDelivered)
        {
            List<RpcRunningCall> calls = _undeliveredCalls;
            _undeliveredCalls = null;
            if (calls == null || !isDelivered) return;
            //
            lock (_callLock)
            {
                foreach (RpcRunningCall call in calls) _calls.Remove(call.Id);
            }
        }
        // The answer of a call that outlasted the wait of its caller. It is handed over until it is delivered
        public string Collect(int callId, int waitSeconds)
        {
            RpcRunningCall call;
            lock (_callLock)
            {
                RemoveOldestCollectableCalls();
                _calls.TryGetValue(callId, out call);
            }
            if (call == null)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "There is no automation call " +
                                       callId.ToString(CultureInfo.InvariantCulture) + " to collect: its " +
                                       "answer was already delivered, it was never started, or it waited too " +
                                       "long - the latest " + MaximumUncollectedCalls.ToString(CultureInfo.InvariantCulture) +
                                       " answers are kept for " +
                                       MaximumUncollectedMinutes.ToString(CultureInfo.InvariantCulture) + " minutes. " +
                                       "Call get_status (status on the pipe) for the calls that are running or " +
                                       "waiting to be collected.");
            }
            return Collect(call, waitSeconds);
        }
        // A caller that never collects must not have every answer kept for the rest of the session: only the
        // latest answers are kept, and none longer than an hour after its call finished
        private void RemoveOldestCollectableCalls()
        {
            DateTime expired = DateTime.Now.AddMinutes(-MaximumUncollectedMinutes);
            List<int> finished = new List<int>();
            foreach (KeyValuePair<int, RpcRunningCall> entry in _calls)
            {
                if (entry.Value.IsFinished) finished.Add(entry.Key);
            }
            finished.Sort();
            for (int i = 0; i < finished.Count; i++)
            {
                if (i < finished.Count - MaximumUncollectedCalls || _calls[finished[i]].EndTime < expired)
                    _calls.Remove(finished[i]);
            }
        }
        public RpcRunningCall GetRunningCall()
        {
            lock (_callLock) return _runningCall;
        }
        // A call that is running or waiting to be collected, or null
        public RpcRunningCall FindCall(int callId)
        {
            RpcRunningCall call;
            lock (_callLock) _calls.TryGetValue(callId, out call);
            return call;
        }
        // The calls still running or still waiting to be collected, oldest first
        public List<RpcRunningCall> GetCalls()
        {
            List<RpcRunningCall> calls;
            lock (_callLock)
            {
                RemoveOldestCollectableCalls();
                calls = new List<RpcRunningCall>(_calls.Values);
            }
            calls.Sort(delegate (RpcRunningCall first, RpcRunningCall second)
            {
                return first.Id.CompareTo(second.Id);
            });
            return calls;
        }
        public static int GetWaitSeconds(RpcRequest request, string name, int defaultSeconds)
        {
            if (!request.Has(name)) return Math.Min(MaximumWaitSeconds, Math.Max(0, defaultSeconds));
            //
            int seconds = request.GetInt(name, -1);
            if (seconds < 0 || seconds > MaximumWaitSeconds)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument " + name + " must be a number of " +
                                       "seconds from 0 to " + MaximumWaitSeconds.ToString(CultureInfo.InvariantCulture) +
                                       ".");
            }
            return seconds;
        }
        // The methods that run whatever Python they are given: they can take as long as the work
        // they were asked for, and they are the ones that change the model
        private static bool ShowsBusyState(string method)
        {
            return method == "exec" || method == "eval";
        }
        private static bool IsSerialized(string method)
        {
            return method != "ping" && method != "status" && method != "shutdown" && method != "wait" &&
                   method != "cancel";
        }
        // Runs a handler on this thread - the connection thread, or the thread of its own an exec or an
        // eval is given - which is where the application already runs scripted API calls. The guard is
        // asked first, on the thread that owns the window, because that is the only place the answer exists
        private string Serve(RpcRequest request, bool requireIdle, Func<RpcRequest, RpcServer, string> handler)
        {
            int timeout = _server.RequestTimeout;
            if (requireIdle) _server.GetUiThread().CheckIdle(timeout);
            return WithReadOnlyBlock(request, delegate ()
            {
                if (!ShowsBusyState(request.Method)) return handler(request, _server);
                //
                return RpcBusyState.Run(_server.Form, Globals.AutomationText,
                                        delegate () { return handler(request, _server); });
            });
        }
        // Runs a handler on the thread that owns the window, for the few that have to be there
        private string ServeOnUiThread(RpcRequest request, bool requireIdle,
                                       Func<RpcRequest, RpcServer, string> handler)
        {
            int timeout = _server.RequestTimeout;
            if (requireIdle) _server.GetUiThread().CheckIdle(timeout);
            return WithReadOnlyBlock(request, delegate ()
            {
                return _server.GetUiThread().Run(timeout, delegate () { return handler(request, _server); });
            });
        }
        // Read only mode does not refuse a method, it blocks the command history: every state change
        // of the application passes through it, so nothing an agent runs can get around the refusal,
        // and reading - which is what an agent mostly does - keeps working. The block is taken only by
        // a call that waits its turn: a method that only reports takes none, so a client polling status
        // does not refuse the user's own edits in between. Calls that report still run beside one that
        // holds the block, and the block is counted, so no call can lift the block another one holds
        private string WithReadOnlyBlock(RpcRequest request, Func<string> handler)
        {
            bool isBlocked = _server.IsReadOnly && IsSerialized(request.Method);
            if (isBlocked) BlockCommands();
            try
            {
                return handler();
            }
            finally
            {
                if (isBlocked) UnblockCommands();
            }
        }
        private void BlockCommands()
        {
            Controller controller = _server.Controller;
            if (controller == null) return;
            //
            lock (_blockLock)
            {
                _blockCount++;
                if (_blockCount == 1) controller.BlockCommands(GetReadOnlyReason());
            }
        }
        private void UnblockCommands()
        {
            Controller controller = _server.Controller;
            if (controller == null) return;
            //
            lock (_blockLock)
            {
                if (_blockCount > 0) _blockCount--;
                if (_blockCount == 0) controller.UnblockCommands();
            }
        }
        // Names where read only mode comes from, because only that source can switch it off
        private string GetReadOnlyReason()
        {
            string reason = "The automation server is in read only mode, so an automation call may not change the model. ";
            if (_server.IsReadOnlyRequested)
                return reason + "This session was started with --rpc ReadOnly, which holds for the whole run.";
            return reason + "Read only is set in Tools - Settings - Automation, where the user can change it.";
        }
        private string Execute(RpcRequest request)
        {
            // Answered on the connection thread: they say nothing about the model, so they are also the
            // calls that still answer while the application is busy
            if (request.Method == "ping") return RpcPingMethod.Execute(request, _server);
            else if (request.Method == "shutdown") return RpcShutdownMethod.Execute(request, _server);
            // Waits its turn like any call that reaches the model, then runs on the connection thread
            else if (request.Method == "exit") return RpcExitMethod.Execute(request, _server);
            // Answered while the call it waits for holds the turn, which is the whole point of it
            else if (request.Method == "wait") return RpcWaitMethod.Execute(request, _server);
            // Stopping a call cannot wait for the call it stops. It changes nothing in the model, so read
            // only mode does not refuse it either
            else if (request.Method == "cancel") return RpcCancelMethod.Execute(request, _server);
            // Not refused while the application is busy - being busy, and saying why, is the answer
            else if (request.Method == "status") return Serve(request, false, RpcStatusMethod.Execute);
            // Refused while the user is doing something, then run on this thread
            else if (request.Method == "describe") return Serve(request, true, RpcDescribeMethod.Execute);
            else if (request.Method == "eval") return Serve(request, true, RpcEvalMethod.Execute);
            else if (request.Method == "exec") return Serve(request, true, RpcExecMethod.Execute);
            else if (request.Method == "history") return Serve(request, true, RpcHistoryMethod.Execute);
            // Only the renderer may read its own buffer, so this one belongs on the window's thread
            else if (request.Method == "screenshot")
                return ServeOnUiThread(request, true, RpcScreenshotMethod.Execute);
            //
            throw new RpcException(RpcErrorCode.MethodNotFound, "The method " + request.Method + " does not exist. " +
                                                                "Call status to see the methods this server serves.");
        }
    }
}
