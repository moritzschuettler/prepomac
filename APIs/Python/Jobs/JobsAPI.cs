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
                   "come from the application settings unless they are given.",
                   Arguments = "name: str, fem_solver: str = None, executable: str = None, " +
                               "argument: str = None, work_directory: str = None, " +
                               "compatibility_mode: bool = None", Returns = "Job",
                   Example = "job = pmx.jobs.add('Job-1')")]
        public JobAPI add(string name, object fem_solver = null, object executable = null,
                          object argument = null, object work_directory = null,
                          object compatibility_mode = null)
        {
            _service.ValidateName(name, null);
            AnalysisJob job = _service.Build(name, fem_solver, executable, argument, work_directory,
                                             compatibility_mode);
            _service.Add(job);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing analysis using command history. A value that is not given is " +
                   "taken from the application settings, not from the analysis being replaced.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add",
                   Returns = "Job")]
        public JobAPI replace(string old_name, string name = null, object fem_solver = null,
                              object executable = null, object argument = null, object work_directory = null,
                              object compatibility_mode = null)
        {
            AnalysisJob oldJob = _service.ResolveJob(old_name);
            AnalysisJob job = _service.Build(name != null ? name : oldJob.Name, fem_solver, executable,
                                             argument, work_directory, compatibility_mode);
            _service.Replace(old_name, job);
            //
            return this[job.Name];
        }

        [PythonAPI("Opens the results of the named analysis using command history. The results become the " +
                   "current result of pmx.results.", Arguments = "name: str", Returns = "None",
                   Example = "pmx.jobs.open_results('Job-1')")]
        public void open_results(string name)
        {
            _service.OpenResults(name);
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
