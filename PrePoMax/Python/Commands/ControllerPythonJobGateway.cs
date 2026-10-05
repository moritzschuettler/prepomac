// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using CaeJob;

namespace PrePoMax.Python
{
    internal class ControllerPythonJobGateway : IPythonJobGateway
    {
        // Variables
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonJobGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public AnalysisJob[] GetAllJobs()
        {
            return _controller.GetAllJobs();
        }
        public AnalysisJob GetJob(string jobName)
        {
            return _controller.GetJob(jobName);
        }
        public string GetSolverExecutable(FEMSolverEnum femSolver)
        {
            if (femSolver == FEMSolverEnum.Abaqus) return _controller.Settings.Solver.AbaqusExe;
            else if (femSolver == FEMSolverEnum.FEMaster) return _controller.Settings.Solver.FEMasterExe;
            else return _controller.Settings.Solver.CalculixExe;
        }
        public string GetWorkDirectory()
        {
            return _controller.Settings.GetWorkDirectory();
        }
        public void AddJob(AnalysisJob job)
        {
            _controller.AddJobCommand(job);
        }
        public void ReplaceJob(string oldJobName, AnalysisJob job)
        {
            _controller.ReplaceJobCommand(oldJobName, job);
        }
        public void DuplicateJobs(string[] jobNames)
        {
            _controller.DuplicateJobsCommand(jobNames);
        }
        public void RemoveJobs(string[] jobNames)
        {
            _controller.RemoveJobsCommand(jobNames);
        }
        public void RenameJob(NamedClass job, string newName)
        {
            // The view is passed explicitly so that a script does not depend on the view the user left behind
            _controller.RenameCommand(job, newName, null, ViewGeometryModelResults.Model);
        }
        public bool PrepareAndRunJob(string jobName, bool onlyCheckModel, bool runSynchronous, int timeoutMilliseconds = 0)
        {
            return _controller.PrepareAndRunJobCommand(jobName, onlyCheckModel, runSynchronous, timeoutMilliseconds);
        }
        public void KillJob(string jobName, string message)
        {
            // Killing an analysis is recorded by no command, so the read only mode refuses it here
            _controller.RefuseWhenCommandsBlocked("Kill analysis");
            AnalysisJob job = _controller.GetJob(jobName);
            if (job != null) job.Kill(message);
        }
        public void OpenResults(string jobName)
        {
            _controller.OpenResultsCommand(jobName);
        }
    }
}
