// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python.Model;
using CaeJob;

namespace APIs.Python
{
    // The analyses of the model. A value that is not given is taken from the application settings, so
    // an analysis added without arguments is the one the dialog would offer.
    public class JobsAPI : ModelItemsAPI<AnalysisJob, JobAPI>
    {
        // Variables
        private readonly JobService _service;


        // Constructors
        internal JobsAPI(JobService service)
            : base("analysis")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds an analysis using command history. The solver executable and the work directory " +
                   "come from the application settings unless they are given; a given one must be a valid path.",
                   Arguments = "name: str, fem_solver: str = None, executable: str = None, " +
                               "argument: str = None, work_directory: str = None, " +
                               "compatibility_mode: bool = None, number_of_cpus: int = None, " +
                               "environment_variables: list[EnvironmentVariable] = None", Returns = "Job",
                   Example = "job = pmx.jobs.add('Job-1')")]
        public JobAPI add(object name, object fem_solver = null, object executable = null,
                          object argument = null, object work_directory = null,
                          object compatibility_mode = null, object number_of_cpus = null, object environment_variables = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            AnalysisJob job = _service.Build(nameText, fem_solver, executable, argument, work_directory,
                                             compatibility_mode, number_of_cpus, environment_variables);
            _service.Add(job);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing analysis using command history. A value that is not given is " +
                   "preserved from the analysis being replaced. Inherit switches restore application defaults. " +
                   "An argument edited away from the solver default is kept through a new name or solver; " +
                   "otherwise the argument follows them.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add; " +
                               "inherit_executable: bool = None, inherit_work_directory: bool = None, " +
                               "inherit_number_of_cpus: bool = None, inherit_environment_variables: bool = None",
                   Returns = "Job")]
        public JobAPI replace(object old_name, object name = null, object fem_solver = null,
                              object executable = null, object argument = null, object work_directory = null,
                              object compatibility_mode = null, object number_of_cpus = null, object environment_variables = null,
                              object inherit_executable = null, object inherit_work_directory = null,
                              object inherit_number_of_cpus = null, object inherit_environment_variables = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            AnalysisJob job = _service.BuildReplacement(oldName, nameText, fem_solver, executable, argument,
                                                        work_directory, compatibility_mode, number_of_cpus,
                                                        environment_variables, inherit_executable,
                                                        inherit_work_directory, inherit_number_of_cpus,
                                                        inherit_environment_variables);
            _service.Replace(oldName, job);
            return this[job.Name];
        }

        [PythonAPI("Opens the results of the named analysis using command history. The results become the " +
                   "current result of pmx.results. A running analysis and a missing results file are refused. " +
                   "A results file modified after the analysis finished raises the question the user interface " +
                   "asks, which an unattended run refuses as a prompt. As the Results action does, the result " +
                   "items the history holds for the analysis are then made again on the new results, as Settings " +
                   "- General - Open results - Run history postprocessing says; inside a recorded script they " +
                   "are not, since the script replays them itself.", Arguments = "name: str", Returns = "None",
                   Example = "pmx.jobs.open_results('Job-1')")]
        public void open_results(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.OpenResults(nameText);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override AnalysisJob GetItem(string name)
        {
            return _service.GetJob(name);
        }
        protected override JobAPI CreateItemAPI(string name)
        {
            return new JobAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(AnalysisJob item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Jobs(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
