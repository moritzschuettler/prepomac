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

namespace PrePoMax
{
    internal class ExecutableJobRunner
    {
        // Variables                                                                                                                
        private ExecutableJob _executableJob;


        // Properties                                                                                                               
        public bool IsRunning { get { return _executableJob != null && _executableJob.JobStatus == JobStatus.Running; } }
        public bool WasKilled { get { return _executableJob != null && _executableJob.JobStatus == JobStatus.Killed; } }


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
            _executableJob.AppendOutput += appendOutput;
            _executableJob.Submit();
            //
            return _executableJob.JobStatus;
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
