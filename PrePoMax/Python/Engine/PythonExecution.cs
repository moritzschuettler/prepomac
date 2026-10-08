// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace PrePoMax.Python
{
    // One run of Python someone wrote - a console line, a script, an automation call - on the thread running it,
    // which is what an interrupt is aimed at. The run is identified by this object rather than by the thread, so
    // an interrupt asked for one run can never reach the next run of the same thread
    public class PythonExecution
    {
        // Variables
        private readonly int _managedThreadId;
        private readonly ulong _pythonThreadId;
        private bool _isInterruptRequested;
        private bool _isInterruptDelivered;


        // Properties
        public int ManagedThreadId { get { return _managedThreadId; } }
        public ulong PythonThreadId { get { return _pythonThreadId; } }
        public bool IsInterruptRequested
        {
            get { return _isInterruptRequested; }
            set { _isInterruptRequested = value; }
        }
        public bool IsInterruptDelivered
        {
            get { return _isInterruptDelivered; }
            set { _isInterruptDelivered = value; }
        }


        // Constructor
        public PythonExecution(int managedThreadId, ulong pythonThreadId)
        {
            _managedThreadId = managedThreadId;
            _pythonThreadId = pythonThreadId;
            _isInterruptRequested = false;
            _isInterruptDelivered = false;
        }
    }
}
