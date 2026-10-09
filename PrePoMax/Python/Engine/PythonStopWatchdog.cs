// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using PrePoMax.Forms;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace PrePoMax.Python
{
    // Offers a stop button while Python from the Python panel runs longer than a moment. The Python keeps
    // running where it always has, on the thread that owns the main window, because every path it can take
    // through the application has been written for that thread; only the stop button lives elsewhere, on a
    // thread of its own with its own message loop, so it answers while the main window does not. Created on
    // the thread about to run the Python and disposed by it when the Python is done
    public class PythonStopWatchdog : IDisposable
    {
        // Variables                                                                                                                
        private const int ShowDelayMilliseconds = 1000;
        private const int DeliveryWaitMilliseconds = 300;
        private readonly object _lock;
        private readonly int _managedThreadId;
        private readonly Rectangle _ownerBounds;
        private readonly System.Threading.Timer _timer;
        private FrmPythonStop _form;
        private PythonExecution _execution;
        private bool _isDisposed;


        // Constructor                                                                                                              
        public PythonStopWatchdog(Rectangle ownerBounds)
        {
            _lock = new object();
            _managedThreadId = Thread.CurrentThread.ManagedThreadId;
            _ownerBounds = ownerBounds;
            _form = null;
            _execution = null;
            _isDisposed = false;
            _timer = new System.Threading.Timer(delegate (object state) { StartStopWindow(); }, null,
                                                ShowDelayMilliseconds, Timeout.Infinite);
        }


        // Methods                                                                                                                  
        private void StartStopWindow()
        {
            lock (_lock)
            {
                if (_isDisposed) return;
            }
            Thread thread = new Thread(RunStopWindow);
            thread.IsBackground = true;
            thread.Name = "PrePoMax Python stop window";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        private void RunStopWindow()
        {
            try
            {
                FrmPythonStop form = new FrmPythonStop(this, _ownerBounds);
                // The handle is created here, on the thread that owns the window, before anyone can be told
                // to close it
                IntPtr handle = form.Handle;
                lock (_lock)
                {
                    if (_isDisposed)
                    {
                        form.Dispose();
                        return;
                    }
                    _form = form;
                }
                Application.Run(form);
            }
            catch (Exception)
            {
                // The stop window is an offer, not part of the run: a window that could not be shown leaves
                // the Python running as it would have without one
            }
        }
        // Interrupts the run this watchdog stands for and no other. The run is the one its thread is in when the
        // first stop is asked for; the thread can start no other run before this watchdog is disposed, and the
        // lock keeps a stop from looking after that. Called from the stop window's thread. Returns false when the
        // run is not going; tells whether the interrupt was delivered within a moment
        public bool RequestStop(out bool isDelivered)
        {
            isDelivered = false;
            PythonExecution execution;
            lock (_lock)
            {
                if (_isDisposed) return false;
                if (_execution == null) _execution = PythonEngineManager.GetExecution(_managedThreadId);
                execution = _execution;
            }
            return PythonEngineManager.RequestInterrupt(execution, DeliveryWaitMilliseconds, out isDelivered);
        }
        // Whether the run can still be stopped, so the stop button is worth offering again: the watchdog stands
        // until the work it watches is done, and that work may not have reached its Python yet
        public bool IsRunning()
        {
            lock (_lock)
            {
                if (_isDisposed) return false;
                return _execution == null || !_execution.IsEnded;
            }
        }
        public void Dispose()
        {
            FrmPythonStop form;
            lock (_lock)
            {
                if (_isDisposed) return;
                _isDisposed = true;
                form = _form;
                _form = null;
            }
            _timer.Dispose();
            if (form != null) form.CloseFromOtherThread();
        }
    }
}
