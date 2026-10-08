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
        private readonly object _lock;
        private readonly int _managedThreadId;
        private readonly Rectangle _ownerBounds;
        private readonly System.Threading.Timer _timer;
        private FrmPythonStop _form;
        private bool _isDisposed;


        // Constructor                                                                                                              
        public PythonStopWatchdog(Rectangle ownerBounds)
        {
            _lock = new object();
            _managedThreadId = Thread.CurrentThread.ManagedThreadId;
            _ownerBounds = ownerBounds;
            _form = null;
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
                FrmPythonStop form = new FrmPythonStop(_managedThreadId, _ownerBounds);
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
