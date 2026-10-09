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
    // an interrupt asked for one run can never reach the next run of the same thread. Its state is guarded by
    // the execution lock of PythonEngineManager
    public class PythonExecution
    {
        // Variables
        private readonly int _managedThreadId;
        private readonly ulong _pythonThreadId;
        private bool _isDeliveryPending;
        private int _deliveredInterruptCount;
        private volatile bool _isEnded;


        // Properties
        public int ManagedThreadId { get { return _managedThreadId; } }
        public ulong PythonThreadId { get { return _pythonThreadId; } }
        // An interrupt was asked for and is waiting for the GIL to be delivered; there is never more than one
        public bool IsDeliveryPending
        {
            get { return _isDeliveryPending; }
            set { _isDeliveryPending = value; }
        }
        // Each Stop or cancel delivers an interrupt of its own, because the run may have caught the one before
        public int DeliveredInterruptCount
        {
            get { return _deliveredInterruptCount; }
            set { _deliveredInterruptCount = value; }
        }
        public bool IsInterruptDelivered { get { return _deliveredInterruptCount > 0; } }
        public bool IsEnded
        {
            get { return _isEnded; }
            set { _isEnded = value; }
        }


        // Constructor
        public PythonExecution(int managedThreadId, ulong pythonThreadId)
        {
            _managedThreadId = managedThreadId;
            _pythonThreadId = pythonThreadId;
            _isDeliveryPending = false;
            _deliveredInterruptCount = 0;
            _isEnded = false;
        }
    }
}
