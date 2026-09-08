// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // The two things a handler needs from the thread that owns the window.
    //
    // CheckIdle asks it whether the user is busy. The question can only be answered there, because an
    // item dialog of this application is not modal and a modal one keeps pumping messages, so a
    // cross thread Invoke returns while one is open; only the thread itself sees the truth.
    //
    // Run puts a handler on it, for the few that have to be there - rendering the viewport is one,
    // because only the renderer may read its own buffer.
    //
    // Everything else runs on the connection thread, which is where the application already runs
    // scripted API calls: a --script run drives the controller from a thread pool thread, and 183 of
    // the 228 public FrmMain methods marshal their own user interface work with InvokeIfRequired. A
    // handler put on the user interface thread instead would freeze the window for as long as it
    // takes, and would deadlock outright on the calls that wait for asynchronous work of their own -
    // opening results goes through OpenAsync(...).Wait(), which can never complete on the thread that
    // has to run its continuation.
    //
    // Both hops are bounded and cancellable: a user interface thread that is inside something which
    // does not pump would otherwise block the connection forever, and a call the connection thread
    // has stopped waiting for must never run afterwards.
    public class RpcUiThread
    {
        // Variables
        private readonly FrmMain _form;


        // Constructor
        public RpcUiThread(FrmMain form)
        {
            _form = form;
        }


        // Methods
        // Refuses when the user is doing something. Nothing has been done when it throws
        public void CheckIdle(int timeoutSeconds)
        {
            string reason = Run(timeoutSeconds, delegate ()
            {
                string busyReason = _form.GetAutomationBusyReason();
                return busyReason == null ? "" : busyReason;
            });
            if (reason.Length > 0) throw new RpcException(RpcErrorCode.Busy, reason);
        }
        public string GetBusyReason(int timeoutSeconds)
        {
            return Run(timeoutSeconds, delegate ()
            {
                string busyReason = _form.GetAutomationBusyReason();
                return busyReason == null ? "" : busyReason;
            });
        }
        public string Run(int timeoutSeconds, Func<string> handler)
        {
            if (_form == null || _form.IsDisposed || !_form.IsHandleCreated)
                throw new RpcException(RpcErrorCode.InternalError, "The application window is not available.");
            //
            RpcCall call = new RpcCall(handler);
            IAsyncResult pending;
            try
            {
                pending = _form.BeginInvoke(new MethodInvoker(call.Run));
            }
            catch (Exception exception)
            {
                throw new RpcException(RpcErrorCode.InternalError, "The request could not be sent to the " +
                                                                   "application: " + exception.Message);
            }
            //
            int timeout = 1000 * Math.Max(1, timeoutSeconds);
            if (!pending.AsyncWaitHandle.WaitOne(timeout))
            {
                if (call.Cancel())
                {
                    throw new RpcException(RpcErrorCode.Busy, "PrePoMax did not answer within " +
                                           timeoutSeconds.ToString(CultureInfo.InvariantCulture) + " s. It is busy " +
                                           "with something that does not release the user interface - a mesher, or " +
                                           "a dialog that is waiting for the user. Nothing was done; call status " +
                                           "and try again.");
                }
                // It started while the wait was being given up on, so it has to be waited out
                pending.AsyncWaitHandle.WaitOne();
            }
            return call.GetResult();
        }
    }
}
