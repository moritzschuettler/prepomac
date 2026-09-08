// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Threading;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // Marks the application busy while an agent call runs, but only once the call has lasted long
    // enough to be worth showing. A handler runs on its connection thread, so the window stays
    // usable while it works - which is what makes it possible for the user to start editing in the
    // middle of a change. Taking the state closes that: it is the same mechanism the application uses
    // for its own long operations, so the menus and the toolbars go quiet and the view stops
    // redrawing until the call is done.
    //
    // The delay is what makes it bearable. A typical call takes a few milliseconds, and a status bar
    // that blinks and menus that flicker disabled twenty times in a conversation read as a fault. So
    // nothing is taken until the call is still running after the delay, and the fast majority passes
    // without touching the window at all.
    //
    // Everything mutable here is touched on the user interface thread only, and both hops are posted
    // rather than waited on, so a call can never be held up by the window it is marking.
    public class RpcBusyState
    {
        // Variables                                                                                                                
        private const int DelayMilliseconds = 300;
        private readonly FrmMain _form;
        private readonly string _text;
        // Written by the calling thread, read by the timer
        private volatile bool _isFinished;
        // The user interface thread owns this one
        private bool _isTaken;
        private System.Threading.Timer _timer;


        // Constructor                                                                                                              
        public RpcBusyState(FrmMain form, string text)
        {
            _form = form;
            _text = text;
            _isFinished = false;
            _isTaken = false;
            _timer = null;
        }


        // Static methods                                                                                                           
        public static string Run(FrmMain form, string text, Func<string> handler)
        {
            RpcBusyState state = new RpcBusyState(form, text);
            state.Begin();
            try
            {
                return handler();
            }
            finally
            {
                state.End();
            }
        }


        // Methods                                                                                                                  
        public void Begin()
        {
            if (_form == null) return;
            _timer = new System.Threading.Timer(OnDelayExpired, null, DelayMilliseconds,
                                                Timeout.Infinite);
        }
        public void End()
        {
            _isFinished = true;
            System.Threading.Timer timer = _timer;
            _timer = null;
            if (timer != null) timer.Dispose();
            // Posted after the take, if there was one, because the queue keeps the order
            Post(Release);
        }
        private void OnDelayExpired(object ignored)
        {
            if (_isFinished) return;
            Post(Take);
        }
        // Both run on the user interface thread
        private void Take()
        {
            if (_isFinished) return;
            _isTaken = _form.SetStateWorking(_text);
        }
        private void Release()
        {
            if (!_isTaken) return;
            _isTaken = false;
            _form.SetStateReady(_text);
        }
        private void Post(MethodInvoker method)
        {
            if (_form == null || _form.IsDisposed || !_form.IsHandleCreated) return;
            //
            try
            {
                _form.BeginInvoke(method);
            }
            catch (Exception)
            {
                // The window is going away, and the state goes with it
            }
        }
    }
}
