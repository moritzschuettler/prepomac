// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using System;
using System.Linq;
using System.Threading;

namespace APIs.Python
{
    internal class JobService
    {
        // Variables
        private readonly IPythonJobGateway _jobGateway;


        // Constructors
        public JobService(IPythonJobGateway jobGateway)
        {
            _jobGateway = jobGateway;
        }


        // Methods
        public string[] GetNames()
        {
            EnsureCommandsAvailable();
            return _jobGateway.GetAllJobs().Select(job => job.Name).ToArray();
        }
        public AnalysisJob GetJob(string name)
        {
            if (name == null) return null;
            //
            EnsureCommandsAvailable();
            return _jobGateway.GetJob(name);
        }
        public AnalysisJob ResolveJob(string name)
        {
            AnalysisJob job = GetJob(name);
            if (job == null) throw new PythonAPIException("The analysis '" + name + "' does not exist.");
            //
            return job;
        }
        public AnalysisJob Build(string name, object femSolver, object executable, object argument,
                                 object workDirectory, object compatibilityMode)
        {
            EnsureCommandsAvailable();
            FEMSolverEnum solver = FEMSolverEnum.Calculix;
            if (femSolver != null)
                solver = PythonValueConverter.ToEnum<FEMSolverEnum>(AsString(femSolver, "fem_solver"),
                                                                     "fem_solver");
            // A new analysis starts from the solver executable and the work directory of the settings,
            // the same way the dialog does
            string executableName = executable != null ? AsString(executable, "executable")
                                                       : _jobGateway.GetSolverExecutable(solver);
            string workDirectoryName = workDirectory != null ? AsString(workDirectory, "work_directory")
                                                             : _jobGateway.GetWorkDirectory();
            // The argument names the input file the solver is given, and defaults to the analysis name
            string argumentValue = argument != null ? AsString(argument, "argument") : name;
            //
            AnalysisJob job = new AnalysisJob(name, executableName, argumentValue, workDirectoryName);
            job.FEMSolver = solver;
            if (compatibilityMode != null)
                job.CompatibilityMode = PythonValueConverter.ToBool(compatibilityMode, "compatibility_mode");
            //
            return job;
        }
        public void Add(AnalysisJob job)
        {
            EnsureCommandsAvailable();
            _jobGateway.AddJob(job);
        }
        public void Replace(string oldName, AnalysisJob job)
        {
            ResolveJob(oldName);
            ValidateName(job.Name, oldName);
            //
            EnsureCommandsAvailable();
            _jobGateway.ReplaceJob(oldName, job);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _jobGateway.DuplicateJobs(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _jobGateway.RemoveJobs(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _jobGateway.RenameJob(item, newName);
        }
        public string Run(string name, bool onlyCheckModel, object wait, object timeout)
        {
            AnalysisJob job = ResolveJob(name);
            bool runSynchronous = wait == null ? true : PythonValueConverter.ToBool(wait, "wait");
            double timeoutSeconds = timeout == null ? 0 : PythonValueConverter.ToDouble(timeout, "timeout");
            if (timeoutSeconds < 0)
                throw new PythonAPIException("The argument 'timeout' must not be negative.");
            if (timeoutSeconds > 0 && !runSynchronous)
                throw new PythonAPIException("A timeout needs 'wait' to be True, because a run that is not " +
                                             "waited for has nothing to time out.");
            //
            EnsureCommandsAvailable();
            // The solver blocks the calling thread, so the timeout is a watchdog that kills the process
            Timer watchdog = null;
            bool timedOut = false;
            try
            {
                if (timeoutSeconds > 0)
                {
                    watchdog = new Timer(state =>
                    {
                        timedOut = true;
                        _jobGateway.KillJob(name, "The analysis was killed after the Python API timeout of " +
                                                  timeoutSeconds + " s.");
                    }, null, (int)(timeoutSeconds * 1000), Timeout.Infinite);
                }
                //
                _jobGateway.PrepareAndRunJob(name, onlyCheckModel, runSynchronous);
            }
            finally
            {
                if (watchdog != null) watchdog.Dispose();
            }
            //
            if (timedOut) return JobStatus.TimedOut.ToString();
            //
            return job.JobStatus.ToString();
        }
        public void Kill(string name)
        {
            ResolveJob(name);
            //
            EnsureCommandsAvailable();
            _jobGateway.KillJob(name, "The analysis was killed from the Python API.");
        }
        public void OpenResults(string name)
        {
            ResolveJob(name);
            //
            EnsureCommandsAvailable();
            _jobGateway.OpenResults(name);
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName, StringComparison.OrdinalIgnoreCase))
                    throw new PythonAPIException("The analysis '" + name + "' already exists.");
            }
        }
        //
        private static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
        private void EnsureCommandsAvailable()
        {
            if (_jobGateway == null)
                throw new PythonAPIException("The analyses are not available in this Python API context.");
        }
    }
}
