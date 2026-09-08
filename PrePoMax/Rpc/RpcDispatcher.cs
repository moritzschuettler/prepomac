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

namespace PrePoMax.Rpc
{
    // The method table. Requests are served one at a time, whichever connection they arrive on, so
    // two agents - or an agent and a script - can never interleave halfway through changing a model.
    // A handler renders its result as a JSON value and the envelope is composed around it; anything
    // it throws that is not an RpcException is an internal error, because it was not foreseen.
    public class RpcDispatcher
    {
        // Variables                                                                                                                
        private readonly RpcServer _server;
        private readonly object _requestLock;


        // Properties                                                                                                               
        public RpcServer Server { get { return _server; } }


        // Constructor                                                                                                              
        public RpcDispatcher(RpcServer server)
        {
            _server = server;
            _requestLock = new object();
        }


        // Methods                                                                                                                  
        public RpcResponse Dispatch(RpcRequest request)
        {
            try
            {
                // A method that only reports is answered while another one is still working, so a
                // client can always ask what is going on; anything that reaches the model waits its
                // turn, whichever connection it arrived on
                if (!IsSerialized(request.Method)) return RpcResponse.FromResult(request.Id, Execute(request));
                //
                lock (_requestLock)
                {
                    return RpcResponse.FromResult(request.Id, Execute(request));
                }
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
        // The methods that run whatever Python they are given: they can take as long as the work
        // they were asked for, and they are the ones that change the model
        private static bool ShowsBusyState(string method)
        {
            return method == "exec" || method == "eval";
        }
        private static bool IsSerialized(string method)
        {
            return method != "ping" && method != "status" && method != "shutdown";
        }
        // Runs a handler on this connection thread, which is where the application already runs
        // scripted API calls. The guard is asked first, on the thread that owns the window, because
        // that is the only place the answer exists
        private string Serve(RpcRequest request, bool requireIdle, Func<RpcRequest, RpcServer, string> handler)
        {
            int timeout = _server.Settings.RequestTimeout;
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
            int timeout = _server.Settings.RequestTimeout;
            if (requireIdle) _server.GetUiThread().CheckIdle(timeout);
            return WithReadOnlyBlock(request, delegate ()
            {
                return _server.GetUiThread().Run(timeout, delegate () { return handler(request, _server); });
            });
        }
        // Read only mode does not refuse a method, it blocks the command history: every state change
        // of the application passes through it, so nothing an agent runs can get around the refusal,
        // and reading - which is what an agent mostly does - keeps working. The block is held for the
        // duration of one call, and calls are serialized, so the user interface is never left
        // refusing the user
        private string WithReadOnlyBlock(RpcRequest request, Func<string> handler)
        {
            bool isReadOnly = _server.IsReadOnly;
            if (isReadOnly) BlockCommands(request.Method);
            try
            {
                return handler();
            }
            finally
            {
                if (isReadOnly) UnblockCommands();
            }
        }
        private void BlockCommands(string method)
        {
            Controller controller = _server.Controller;
            if (controller == null) return;
            controller.BlockCommands("The automation server is in read only mode, so " + method + " may not " +
                                     "change the model. The user can change that in Tools - Settings - Automation.");
        }
        private void UnblockCommands()
        {
            Controller controller = _server.Controller;
            if (controller != null) controller.UnblockCommands();
        }
        private string Execute(RpcRequest request)
        {
            // Answered on the pipe thread: they say nothing about the model, so they are also the
            // calls that still answer while the application is busy
            if (request.Method == "ping") return RpcPingMethod.Execute(request, _server);
            else if (request.Method == "shutdown") return RpcShutdownMethod.Execute(request, _server);
            else if (request.Method == "exit") return RpcExitMethod.Execute(request, _server);
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
