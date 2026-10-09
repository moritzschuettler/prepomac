// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using CaeJob;

namespace PrePoMax.Python.Journal
{
    // An analysis takes its solver executable, its work directory, its processor count and its environment from the
    // application settings unless the user overrode them, and the job records which of them it did override. Only
    // those are emitted, so that a journal replayed on another machine still picks up its own settings. A replace
    // keeps what it is not given, so it writes the solver, the argument and the compatibility mode always and
    // switches every setting that is not overridden back to the application default.
    public static class PythonJobEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, AnalysisJob job)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(job.Name) };
            if (job.FEMSolver != FEMSolverEnum.Calculix)
                arguments.Add("fem_solver=" + PythonValueFormatter.Text(job.FEMSolver.ToString()));
            if (job.ExecutableOverridden)
                arguments.Add("executable=" + PythonValueFormatter.Path(job.Executable));
            if (job.Argument != AnalysisJob.GetDefaultArgument(job.FEMSolver, job.Name))
                arguments.Add("argument=" + PythonValueFormatter.Text(job.Argument));
            if (job.WorkDirectoryOverridden)
                arguments.Add("work_directory=" + PythonValueFormatter.Path(job.WorkDirectory));
            if (job.CompatibilityMode) arguments.Add("compatibility_mode=True");
            if (job.NumCPUsOverridden) arguments.Add("number_of_cpus=" + job.NumCPUs);
            if (job.EnvironmentVariablesOverridden) arguments.Add(GetEnvironmentVariables(job));
            //
            writer.Statement(writer.Call("pmx.jobs", "add", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public static void Replace(PythonJournalWriter writer, string oldName, AnalysisJob job)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(oldName),
                                                        PythonValueFormatter.Text(job.Name) };
            arguments.Add("fem_solver=" + PythonValueFormatter.Text(job.FEMSolver.ToString()));
            if (job.ExecutableOverridden)
                arguments.Add("executable=" + PythonValueFormatter.Path(job.Executable));
            arguments.Add("argument=" + PythonValueFormatter.Text(job.Argument));
            if (job.WorkDirectoryOverridden)
                arguments.Add("work_directory=" + PythonValueFormatter.Path(job.WorkDirectory));
            arguments.Add("compatibility_mode=" + PythonValueFormatter.Format(job.CompatibilityMode));
            if (job.NumCPUsOverridden) arguments.Add("number_of_cpus=" + job.NumCPUs);
            if (job.EnvironmentVariablesOverridden) arguments.Add(GetEnvironmentVariables(job));
            //
            if (!job.ExecutableOverridden) arguments.Add("inherit_executable=True");
            if (!job.WorkDirectoryOverridden) arguments.Add("inherit_work_directory=True");
            if (!job.NumCPUsOverridden) arguments.Add("inherit_number_of_cpus=True");
            if (!job.EnvironmentVariablesOverridden) arguments.Add("inherit_environment_variables=True");
            //
            writer.Statement(writer.Call("pmx.jobs", "replace", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public static void Run(PythonJournalWriter writer, string jobName, bool onlyCheckModel)
        {
            string job = "pmx.jobs[" + PythonValueFormatter.Text(jobName) + "]";
            //
            writer.Statement(writer.Call(job, onlyCheckModel ? "check_model" : "run", "wait=True"));
            writer.CountEmittedCommand();
        }
        private static string GetEnvironmentVariables(AnalysisJob job)
        {
            List<string> makers = new List<string>();
            if (job.EnvironmentVariables != null)
            {
                foreach (EnvironmentVariable variable in job.EnvironmentVariables)
                    makers.Add(PythonJournalWriter.Join("pmx.create.environment_variable",
                                                        PythonValueFormatter.Text(variable.Name),
                                                        PythonValueFormatter.Text(variable.Value),
                                                        variable.Active ? null : "active=False"));
            }
            return "environment_variables=[" + string.Join(", ", makers) + "]";
        }
    }
}
