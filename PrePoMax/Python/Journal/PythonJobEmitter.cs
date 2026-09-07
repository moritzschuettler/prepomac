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
    // An analysis takes its solver, its work directory and its processor count from the application
    // settings unless the user overrode them, and the job records which of them it did override. Only
    // those are emitted, so that a journal replayed on another machine still picks up its own settings.
    public static class PythonJobEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, AnalysisJob job)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(job.Name) };
            AddOverriddenValues(arguments, job);
            //
            writer.Statement(writer.Call("pmx.jobs", "add", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public static void Replace(PythonJournalWriter writer, string oldName, AnalysisJob job)
        {
            List<string> arguments = new List<string> { PythonValueFormatter.Text(oldName),
                                                        PythonValueFormatter.Text(job.Name) };
            AddOverriddenValues(arguments, job);
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
        private static void AddOverriddenValues(List<string> arguments, AnalysisJob job)
        {
            if (job.ExecutableOverridden)
            {
                arguments.Add("executable=" + PythonValueFormatter.Path(job.Executable));
                arguments.Add("argument=" + PythonValueFormatter.Text(job.Argument));
            }
            if (job.WorkDirectoryOverridden)
                arguments.Add("work_directory=" + PythonValueFormatter.Path(job.WorkDirectory));
            if (job.NumCPUsOverridden) arguments.Add("num_cpus=" + job.NumCPUs);
            if (job.CompatibilityMode) arguments.Add("compatibility_mode=True");
            if (job.EnvironmentVariablesOverridden && job.EnvironmentVariables != null &&
                job.EnvironmentVariables.Count > 0)
            {
                List<string> makers = new List<string>();
                foreach (EnvironmentVariable variable in job.EnvironmentVariables)
                    makers.Add(PythonJournalWriter.Join("pmx.create.environment_variable",
                                                        PythonValueFormatter.Text(variable.Name),
                                                        PythonValueFormatter.Text(variable.Value),
                                                        variable.Active ? null : "active=False"));
                arguments.Add("environment_variables=[" + string.Join(", ", makers) + "]");
            }
        }
    }
}
