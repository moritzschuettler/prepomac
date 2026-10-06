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
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PrePoMax.Rpc
{
    // A call that runs on a thread of its own, so that its caller can be answered while it is still
    // working. Meshing a large part, importing a file or regenerating a model can take longer than any
    // client is prepared to hold a request open, and a client that gives up is not the end of the
    // work: the call runs to the end, records what it changes like any other, and keeps its answer
    // until it is collected. Until then it holds the turn, so nothing else reaches the model halfway
    // through it.
    //
    // The thread is an ordinary one rather than the thread that owns the window, for the reasons the
    // connection thread is: the application already runs scripted API calls off that thread and
    // marshals its own user interface work.
    public class RpcRunningCall
    {
        // Variables                                                                                                                
        private readonly int _id;
        private readonly string _method;
        private readonly DateTime _startTime;
        private readonly object _stateLock;
        private DateTime _endTime;
        private volatile bool _isFinished;
        private string _result;
        private ExceptionDispatchInfo _exception;


        // Properties                                                                                                               
        public int Id { get { return _id; } }
        public string Method { get { return _method; } }
        public bool IsFinished { get { return _isFinished; } }
        public double Elapsed
        {
            get
            {
                DateTime end = _isFinished ? _endTime : DateTime.Now;
                return (end - _startTime).TotalSeconds;
            }
        }


        // Constructor                                                                                                              
        public RpcRunningCall(int id, string method)
        {
            _id = id;
            _method = method;
            _startTime = DateTime.Now;
            _stateLock = new object();
            _endTime = DateTime.MinValue;
            _isFinished = false;
            _result = null;
            _exception = null;
        }


        // Methods                                                                                                                  
        // The finished callback runs before a waiting caller is woken, so the turn it gives back is
        // free by the time the caller can send its next call
        public void Start(Func<string> handler, Action finished)
        {
            Thread thread = new Thread(delegate () { Run(handler, finished); });
            thread.IsBackground = true;
            thread.Name = "PrePoMax automation call " + _id.ToString(CultureInfo.InvariantCulture);
            thread.Start();
        }
        private void Run(Func<string> handler, Action finished)
        {
            try
            {
                _result = handler();
            }
            catch (Exception exception)
            {
                _exception = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                try
                {
                    finished();
                }
                finally
                {
                    lock (_stateLock)
                    {
                        _endTime = DateTime.Now;
                        _isFinished = true;
                        Monitor.PulseAll(_stateLock);
                    }
                }
            }
        }
        public bool Wait(int milliseconds)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, milliseconds));
            lock (_stateLock)
            {
                while (!_isFinished)
                {
                    int left = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (left <= 0) return false;
                    Monitor.Wait(_stateLock, left);
                }
                return true;
            }
        }
        // The answer the call would have given had the caller waited for it, refusal and all
        public string GetResult()
        {
            if (_exception != null) _exception.Throw();
            return _result;
        }
        public string GetRunningAnswer()
        {
            string id = _id.ToString(CultureInfo.InvariantCulture);
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("running", true);
            writer.WriteValue("callId", _id);
            writer.WriteValue("method", _method);
            writer.WriteValue("elapsed", Elapsed);
            writer.WriteValue("message", "The call is still running after " + FormatSeconds(Elapsed) + ". It has " +
                                         "not been abandoned: it runs to the end, and what it changes is recorded " +
                                         "as usual. Collect its answer with get_call - the wait method on the pipe " +
                                         "- and call id " + id + ". Every call that would change the model waits " +
                                         "for it until it is done.");
            writer.EndObject();
            return writer.ToString();
        }
        public string GetBusyReason()
        {
            return "Automation call " + _id.ToString(CultureInfo.InvariantCulture) + " (" + _method + ") has been " +
                   "running for " + FormatSeconds(Elapsed) + ". Collect its answer with get_call.";
        }
        public static string FormatSeconds(double seconds)
        {
            return seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }
    }
}
