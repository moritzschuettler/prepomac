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
using System.Collections.Generic;
using System.Linq;

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
            if (_jobGateway == null) return new string[0];
            return _jobGateway.GetAllJobs().Select(job => job.Name).ToArray();
        }
        public AnalysisJob GetJob(string name)
        {
            if (name == null || _jobGateway == null) return null;
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
                                 object workDirectory, object compatibilityMode, object numCPUs = null,
                                 object environmentVariables = null)
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
            job.ExecutableOverridden = executable != null;
            job.WorkDirectoryOverridden = workDirectory != null;
            job.FEMSolver = solver;
            if (argument != null) job.Argument = argumentValue;
            if (compatibilityMode != null)
                job.CompatibilityMode = PythonValueConverter.ToBool(compatibilityMode, "compatibility_mode");
            //
            ApplyExecutionSettings(job, numCPUs, environmentVariables);
            return job;
        }
        public AnalysisJob BuildReplacement(string oldName, string name, object femSolver, object executable,
                                            object argument, object workDirectory, object compatibilityMode,
                                            object numCPUs, object environmentVariables,
                                            object inheritExecutable, object inheritWorkDirectory,
                                            object inheritNumCPUs, object inheritEnvironment)
        {
            EnsureCommandsAvailable();
            AnalysisJob existing = ResolveJob(oldName);
            if (existing.JobStatus == JobStatus.Running || existing.JobStatus == JobStatus.InQueue)
                throw new PythonAPIException("A running analysis cannot be replaced.");
            if (name != null) ValidateName(name, oldName);
            AnalysisJob job = existing.DeepClone();
            job.Name = name ?? existing.Name;
            if (femSolver != null)
                job.FEMSolver = PythonValueConverter.ToEnum<FEMSolverEnum>(AsString(femSolver, "fem_solver"),
                                                                           "fem_solver");
            job.Argument = argument == null ? existing.Argument : AsString(argument, "argument");
            if (executable != null)
            {
                job.Executable = AsString(executable, "executable");
                job.ExecutableOverridden = true;
            }
            if (workDirectory != null)
            {
                job.WorkDirectory = AsString(workDirectory, "work_directory");
                job.WorkDirectoryOverridden = true;
            }
            if (compatibilityMode != null)
                job.CompatibilityMode = PythonValueConverter.ToBool(compatibilityMode, "compatibility_mode");
            ApplyExecutionSettings(job, numCPUs, environmentVariables);
            ApplyInheritance(job, inheritExecutable, executable, "executable");
            ApplyInheritance(job, inheritWorkDirectory, workDirectory, "work_directory");
            ApplyInheritance(job, inheritNumCPUs, numCPUs, "number_of_cpus");
            ApplyInheritance(job, inheritEnvironment, environmentVariables, "environment_variables");
            return job;
        }
        private static void ApplyInheritance(AnalysisJob job, object inherit, object supplied, string name)
        {
            if (inherit == null) return;
            bool useDefault = PythonValueConverter.ToBool(inherit, "inherit_" + name);
            if (useDefault && supplied != null)
                throw new PythonAPIException("The argument '" + name + "' cannot be given together with " +
                                             "'inherit_" + name + "'.");
            if (name == "executable") job.ExecutableOverridden = !useDefault;
            else if (name == "work_directory") job.WorkDirectoryOverridden = !useDefault;
            else if (name == "number_of_cpus") job.NumCPUsOverridden = !useDefault;
            else job.EnvironmentVariablesOverridden = !useDefault;
        }
        private static void ApplyExecutionSettings(AnalysisJob job, object numCPUs, object environmentVariables)
        {
            if (numCPUs != null)
            {
                int count = PythonValueConverter.ToInt(numCPUs, "number_of_cpus");
                if (count < 1) throw new PythonAPIException("The argument 'number_of_cpus' must be positive.");
                job.NumCPUs = count;
                job.NumCPUsOverridden = true;
            }
            if (environmentVariables != null)
            {
                List<EnvironmentVariable> variables =
                    PythonObjectListConverter.ToList<EnvironmentVariable>(environmentVariables,
                                                                          "environment_variables",
                                                                          "a list of environment variables");
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (EnvironmentVariable variable in variables)
                {
                    ValidateEnvironmentVariable(variable.Name, variable.Value);
                    if (!names.Add(variable.Name))
                        throw new PythonAPIException("Environment variable names must be unique.");
                }
                job.EnvironmentVariables = variables.DeepClone();
                job.EnvironmentVariablesOverridden = true;
            }
        }
        internal static void ValidateEnvironmentVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOf('=') >= 0 || name.IndexOf('\0') >= 0)
                throw new PythonAPIException("An environment variable name must not be empty and must contain " +
                                             "neither an equals sign nor a NUL character.");
            if (value == null || value.IndexOf('\0') >= 0)
                throw new PythonAPIException("An environment variable value must be a string without a NUL " +
                                             "character.");
        }
        public void Add(AnalysisJob job)
        {
            EnsureCommandsAvailable();
            _jobGateway.AddJob(job);
        }
        public void Replace(string oldName, AnalysisJob job)
        {
            EnsureCommandsAvailable();
            ResolveJob(oldName);
            ValidateName(job.Name, oldName);
            //
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
            EnsureCommandsAvailable();
            AnalysisJob job = ResolveJob(name);
            bool runSynchronous = wait == null ? true : PythonValueConverter.ToBool(wait, "wait");
            double timeoutSeconds = timeout == null ? 0 : PythonValueConverter.ToFiniteDouble(timeout, "timeout");
            if (timeoutSeconds < 0 || timeoutSeconds > (int.MaxValue - 1) / 1000.0)
                throw new PythonAPIException("The argument 'timeout' must be a finite number of seconds " +
                                             "between 0 and 2147483.646.");
            if (timeoutSeconds > 0 && !runSynchronous)
                throw new PythonAPIException("A timeout needs 'wait' to be True, because a run that is not " +
                                             "waited for has nothing to time out.");
            //
            if (job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.InQueue)
                throw new PythonAPIException("The analysis is already running.");
            int milliseconds = timeoutSeconds == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(timeoutSeconds * 1000));
            if (!_jobGateway.PrepareAndRunJob(name, onlyCheckModel, runSynchronous, milliseconds))
                throw new PythonAPIException("The analysis was not started because model preparation was refused.");
            return job.JobStatus.ToString();
        }
        public void Kill(string name)
        {
            EnsureCommandsAvailable();
            ResolveJob(name);
            //
            _jobGateway.KillJob(name, "The analysis was killed from the Python API.");
        }
        public void OpenResults(string name)
        {
            EnsureCommandsAvailable();
            ResolveJob(name);
            //
            _jobGateway.OpenResults(name);
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
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
