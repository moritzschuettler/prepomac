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

namespace APIs.Python
{
    public interface IPythonJobGateway
    {
        AnalysisJob[] GetAllJobs();
        AnalysisJob GetJob(string jobName);
        // The application settings carry the solver defaults a new analysis starts from
        string GetSolverExecutable(FEMSolverEnum femSolver);
        string GetWorkDirectory();
        //
        void AddJob(AnalysisJob job);
        void ReplaceJob(string oldJobName, AnalysisJob job);
        void DuplicateJobs(string[] jobNames);
        void RemoveJobs(string[] jobNames);
        void RenameJob(NamedClass job, string newName);
        // A run started with runSynchronous returns only when the solver has finished
        bool PrepareAndRunJob(string jobName, bool onlyCheckModel, bool runSynchronous);
        void KillJob(string jobName, string message);
        void OpenResults(string jobName);
    }
}
