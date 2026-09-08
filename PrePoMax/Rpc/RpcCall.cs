// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PrePoMax.Rpc
{
    // One handler on its way to the user interface thread. The state is what makes a timeout safe: a
    // call the pipe thread has given up on is cancelled before it starts, so it cannot run minutes
    // later against a model the user has meanwhile changed. Cancelling and starting race, and exactly
    // one of them wins.
    public class RpcCall
    {
        // Variables                                                                                                                
        private const int Pending = 0;
        private const int Running = 1;
        private const int Cancelled = 2;
        private readonly Func<string> _handler;
        private int _state;
        private string _result;
        private ExceptionDispatchInfo _exception;


        // Properties                                                                                                               
        public bool IsCancelled { get { return _state == Cancelled; } }


        // Constructor                                                                                                              
        public RpcCall(Func<string> handler)
        {
            _handler = handler;
            _state = Pending;
            _result = null;
            _exception = null;
        }


        // Methods                                                                                                                  
        // Runs on the user interface thread
        public void Run()
        {
            if (Interlocked.CompareExchange(ref _state, Running, Pending) != Pending) return;
            //
            try
            {
                _result = _handler();
            }
            catch (Exception exception)
            {
                // Control.Invoke rethrows with a plain throw, which would replace the stack of the
                // frames that actually failed with the stack of the marshalling call
                _exception = ExceptionDispatchInfo.Capture(exception);
            }
        }
        // Returns true when the call had not started yet and will now never run
        public bool Cancel()
        {
            return Interlocked.CompareExchange(ref _state, Cancelled, Pending) == Pending;
        }
        public string GetResult()
        {
            if (_exception != null) _exception.Throw();
            return _result;
        }
    }
}
