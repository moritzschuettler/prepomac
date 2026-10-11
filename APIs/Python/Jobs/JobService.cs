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
using System.IO;
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
            return _jobGateway.GetAllJobs().FirstOrDefault(job => PythonNameComparer.AreSame(job.Name, name));
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
            string executableName = executable != null ? AsPath(executable, "executable")
                                                       : _jobGateway.GetSolverExecutable(solver);
            string workDirectoryName = workDirectory != null ? AsPath(workDirectory, "work_directory")
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
            if (name != null) ValidateName(name, existing.Name);
            AnalysisJob job = existing.DeepClone();
            job.Name = name ?? existing.Name;
            if (femSolver != null)
                job.FEMSolver = PythonValueConverter.ToEnum<FEMSolverEnum>(AsString(femSolver, "fem_solver"),
                                                                           "fem_solver");
            // An argument the user typed survives a new name or solver; the solver default follows them both
            bool argumentEdited = existing.Argument != AnalysisJob.GetDefaultArgument(existing.FEMSolver, existing.Name);
            if (argument != null) job.Argument = AsString(argument, "argument");
            else if (argumentEdited) job.Argument = existing.Argument;
            else job.Argument = AnalysisJob.GetDefaultArgument(job.FEMSolver, job.Name);
            if (executable != null)
            {
                job.Executable = AsPath(executable, "executable");
                job.ExecutableOverridden = true;
            }
            if (workDirectory != null)
            {
                job.WorkDirectory = AsPath(workDirectory, "work_directory");
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
                    PythonObjectListConverter.ToList<EnvironmentVariableAPI>(environmentVariables,
                                                                             "environment_variables",
                                                                             "a list of environment variables " +
                                                                             "created by " +
                                                                             "pmx.create.environment_variable")
                                             .Select(variable => variable.GetVariableCopy()).ToList();
                HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (EnvironmentVariable variable in variables)
                {
                    ValidateEnvironmentVariable(variable.Name, variable.Value);
                    if (!names.Add(variable.Name))
                        throw new PythonAPIException("Environment variable names must be unique.");
                }
                job.EnvironmentVariables = variables;
                job.EnvironmentVariablesOverridden = true;
            }
        }
        internal static void ValidateEnvironmentVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new PythonAPIException("The argument 'name' of an environment variable must not be empty.");
            if (name.IndexOf('=') >= 0 || name.IndexOf('\0') >= 0)
                throw new PythonAPIException("The argument 'name' of an environment variable must contain neither " +
                                             "an equals sign nor a NUL character.");
            if (string.IsNullOrWhiteSpace(value))
                throw new PythonAPIException("The argument 'value' of an environment variable must not be empty.");
            if (value.IndexOf('\0') >= 0)
                throw new PythonAPIException("The argument 'value' of an environment variable must not contain a " +
                                             "NUL character.");
        }
        public void Add(AnalysisJob job)
        {
            EnsureCommandsAvailable();
            _jobGateway.AddJob(job);
        }
        public void Replace(string oldName, AnalysisJob job)
        {
            EnsureCommandsAvailable();
            string storedOldName = ResolveJob(oldName).Name;
            ValidateName(job.Name, storedOldName);
            //
            _jobGateway.ReplaceJob(storedOldName, job);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _jobGateway.DuplicateJobs(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            foreach (string name in names) EnsureNotRunning(ResolveJob(name), "removed");
            _jobGateway.RemoveJobs(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            EnsureNotRunning(ResolveJob(item.Name), "renamed");
            // A renamed analysis is given the default argument of its new name, while a replace keeps an argument
            // the user edited, so an edited argument is renamed the way a replace renames it
            AnalysisJob job = item as AnalysisJob;
            if (job != null && job.Argument != AnalysisJob.GetDefaultArgument(job.FEMSolver, job.Name))
            {
                Replace(job.Name, BuildReplacement(job.Name, newName, null, null, null, null, null, null, null, null,
                                                   null, null, null));
                return;
            }
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
            EnsureRunnable(job);
            int milliseconds =timeoutSeconds == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(timeoutSeconds * 1000));
            int numOfMessagesBefore = MessageBoxes.GetReportedMessages().Length;
            if (!_jobGateway.PrepareAndRunJob(job.Name, onlyCheckModel, runSynchronous, milliseconds))
            {
                // The preparation says why in a message box, which a call of the automation server is answered
                // instead of being shown
                string[] messages = MessageBoxes.GetReportedMessages().Skip(numOfMessagesBefore).ToArray();
                string reason = messages.Length == 0 ? "" : " " + string.Join(" ", messages);
                throw new PythonAPIException("The analysis was not started because model preparation was refused." +
                                             reason);
            }
            return job.JobStatus.ToString();
        }
        // The run refuses a missing work directory and a missing solver executable only after it was recorded, so
        // the same states are refused here, by the directory and the executable the run would use
        private void EnsureRunnable(AnalysisJob job)
        {
            bool ownWorkDirectory = job.WorkDirectory != null && job.WorkDirectory.Trim().Length > 0;
            string workDirectory;
            try
            {
                string inputFileName = Path.Combine(ownWorkDirectory ? job.WorkDirectory
                                                                     : _jobGateway.GetWorkDirectory(),
                                                    job.Name + ".inp");
                workDirectory = Path.GetDirectoryName(inputFileName);
            }
            catch (Exception exception) when (IsPathException(exception))
            {
                throw new PythonAPIException("The work directory of the analysis '" + job.Name + "' is not a valid " +
                                             "path: " + exception.Message + " " + GetWorkDirectoryHint(ownWorkDirectory));
            }
            if (workDirectory == null || !Directory.Exists(workDirectory))
                throw new PythonAPIException("The work directory '" + workDirectory + "' of the analysis '" + job.Name +
                                             "' does not exist. " + GetWorkDirectoryHint(ownWorkDirectory));
            if (!File.Exists(job.Executable))
                throw new PythonAPIException("The executable file '" + job.Executable + "' of the analysis '" +
                                             job.Name + "' does not exist. " +
                                             (job.ExecutableOverridden
                                              ? "Give it an existing 'executable' with pmx.jobs.replace."
                                              : "Set the solver executable in the application settings, or give " +
                                                "the analysis its own 'executable' with pmx.jobs.replace."));
        }
        private static string GetWorkDirectoryHint(bool ownWorkDirectory)
        {
            return ownWorkDirectory
                   ? "Create it, or give the analysis an existing 'work_directory' with pmx.jobs.replace."
                   : "Set the work directory in the application settings, or give the analysis its own " +
                     "'work_directory' with pmx.jobs.replace.";
        }
        public void Kill(string name)
        {
            EnsureCommandsAvailable();
            string storedName = ResolveJob(name).Name;
            //
            _jobGateway.KillJob(storedName, "The analysis was killed from the Python API.");
        }
        public void OpenResults(string name)
        {
            EnsureCommandsAvailable();
            AnalysisJob job = ResolveJob(name);
            if (job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.InQueue)
                throw new PythonAPIException("The analysis '" + job.Name + "' is still running. Open its results " +
                                             "once it has finished.");
            string resultsFileName = GetResultsFileName(job);
            if (!File.Exists(resultsFileName))
                throw new PythonAPIException("The results file of the analysis '" + job.Name + "' does not exist: " +
                                             resultsFileName);
            //
            _jobGateway.OpenResults(job.Name);
        }
        public string GetResultsFileName(AnalysisJob job)
        {
            try
            {
                string resultsFileName = job.ResultsFileName;
                Path.GetFullPath(resultsFileName);
                return resultsFileName;
            }
            catch (Exception exception) when (IsPathException(exception))
            {
                throw new PythonAPIException("The work directory of the analysis '" + job.Name + "' is not a valid " +
                                             "path: " + exception.Message);
            }
        }
        public bool IsUpToDate(AnalysisJob job)
        {
            GetResultsFileName(job);
            return job.IsUpToDate;
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            // Renaming an analysis by case only keeps its own name, which no other analysis holds
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
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
        private static string AsPath(object value, string argumentName)
        {
            string path = AsString(value, argumentName);
            try
            {
                Path.GetFullPath(path);
            }
            catch (Exception exception) when (IsPathException(exception))
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is not a valid path: '" + path +
                                             "'. " + exception.Message);
            }
            //
            return path;
        }
        private static bool IsPathException(Exception exception)
        {
            return exception is ArgumentException || exception is NotSupportedException ||
                   exception is PathTooLongException || exception is System.Security.SecurityException;
        }
        private static bool IsRunning(AnalysisJob job)
        {
            return job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.InQueue;
        }
        private static void EnsureNotRunning(AnalysisJob job, string participle)
        {
            if (IsRunning(job))
                throw new PythonAPIException("The analysis '" + job.Name + "' is running and cannot be " + participle +
                                             ". Wait for it to finish or kill it first.");
        }
        internal static void EnsureNoAnalysisRunning(IPythonJobGateway jobGateway, string operation)
        {
            if (jobGateway == null) return;
            AnalysisJob runningJob = jobGateway.GetAllJobs().FirstOrDefault(IsRunning);
            if (runningJob != null)
                throw new PythonAPIException(operation + " is refused while the analysis '" + runningJob.Name +
                                             "' is running. Wait for it to finish or kill it first.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_jobGateway == null)
                throw new PythonAPIException("The analyses are not available in this Python API context.");
        }
    }
}
