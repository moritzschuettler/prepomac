// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeJob;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace PrePoMax
{
    internal class ExecutableJobRunner
    {
        // Variables                                                                                                                
        private ExecutableJob _executableJob;


        // Properties                                                                                                               
        public bool IsRunning { get { return _executableJob != null && _executableJob.JobStatus == JobStatus.Running; } }
        public bool WasKilled { get { return _executableJob != null && _executableJob.JobStatus == JobStatus.Killed; } }
        public string ErrorData { get { return _executableJob == null ? null : _executableJob.ErrorData; } }


        // Constructors                                                                                                             
        public ExecutableJobRunner()
        {
            _executableJob = null;
        }


        // Methods                                                                                                                  
        public JobStatus Run(string name, string executable, string argument, string workDirectory,
                             Action<string> appendOutput)
        {
            _executableJob = new ExecutableJob(name, executable, argument, workDirectory);
            // A job run from the user interface thread holds that thread until the job ends, while the output
            // the job reports from its reading thread is marshalled to the output window on it - which waits
            // for the job forever. That output is held and written by the calling thread instead, in order.
            Action<string> output = appendOutput;
            List<string> heldOutput = null;
            if (appendOutput != null && Application.MessageLoop)
            {
                int callingThreadId = Thread.CurrentThread.ManagedThreadId;
                heldOutput = new List<string>();
                output = data =>
                {
                    if (Thread.CurrentThread.ManagedThreadId != callingThreadId)
                    {
                        lock (heldOutput) heldOutput.Add(data);
                        return;
                    }
                    WriteHeldOutput(heldOutput, appendOutput);
                    appendOutput(data);
                };
            }
            _executableJob.AppendOutput += output;
            try
            {
                _executableJob.Submit();
            }
            finally
            {
                if (heldOutput != null) WriteHeldOutput(heldOutput, appendOutput);
            }
            //
            return _executableJob.JobStatus;
        }
        private static void WriteHeldOutput(List<string> heldOutput, Action<string> appendOutput)
        {
            string[] data;
            lock (heldOutput)
            {
                data = heldOutput.ToArray();
                heldOutput.Clear();
            }
            foreach (string item in data) appendOutput(item);
        }
        public bool Kill(string message)
        {
            if (!IsRunning) return false;
            //
            _executableJob.Kill(message);
            return true;
        }
    }
}
