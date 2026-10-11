// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeJob;
using CaeGlobals;
using System.Collections.Generic;

namespace APIs.Python
{
    public class JobAPI : BaseAPI
    {
        // Variables
        private readonly string _jobName;
        private readonly JobService _service;


        // Properties
        [PythonAPI("The analysis name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The FEM solver: Calculix, Abaqus or FEMaster.", Returns = "str")]
        public string fem_solver { get { return Resolve().FEMSolver.ToString(); } }
        [PythonAPI("The solver executable file.", Returns = "str")]
        public string executable { get { return Resolve().Executable; } }
        [PythonAPI("The command line arguments the solver is given. It names the input file.",
                   Returns = "str")]
        public string argument { get { return Resolve().Argument; } }
        [PythonAPI("The work directory the analysis runs in.", Returns = "str")]
        public string work_directory { get { return Resolve().WorkDirectory; } }
        [PythonAPI("True when the solver runs in the simplified compatibility mode.", Returns = "bool")]
        public bool compatibility_mode { get { return Resolve().CompatibilityMode; } }
        [PythonAPI("The number of solver CPUs.", Returns = "int")]
        public int number_of_cpus { get { return Resolve().NumCPUs; } }
        [PythonAPI("Detached copies of the solver environment variable definitions, as a plain list that goes " +
                   "back into the argument environment_variables of add and replace.",
                   Returns = "list[EnvironmentVariable]")]
        public object environment_variables
        {
            get
            {
                List<EnvironmentVariable> environmentVariables = Resolve().EnvironmentVariables;
                if (environmentVariables == null) return PythonListConverter.ToPythonList(new object[0]);
                object[] variables = new object[environmentVariables.Count];
                for (int i = 0; i < variables.Length; i++)
                    variables[i] = new EnvironmentVariableAPI(environmentVariables[i].DeepClone());
                return PythonListConverter.ToPythonList(variables);
            }
        }
        [PythonAPI("True when the solver executable follows the application settings, False when the analysis " +
                   "overrides it. Give it back as inherit_executable of replace, rather than executable, so a " +
                   "replace keeps it following the settings.", Returns = "bool")]
        public bool inherit_executable { get { return !Resolve().ExecutableOverridden; } }
        [PythonAPI("True when the work directory follows the application settings, False when the analysis " +
                   "overrides it, which inherit_work_directory of replace takes back.", Returns = "bool")]
        public bool inherit_work_directory { get { return !Resolve().WorkDirectoryOverridden; } }
        [PythonAPI("True when the number of solver CPUs follows the application settings, False when the " +
                   "analysis overrides it, which inherit_number_of_cpus of replace takes back.", Returns = "bool")]
        public bool inherit_number_of_cpus { get { return !Resolve().NumCPUsOverridden; } }
        [PythonAPI("True when the solver environment variables follow the application settings, False when the " +
                   "analysis overrides them, which inherit_environment_variables of replace takes back.",
                   Returns = "bool")]
        public bool inherit_environment_variables { get { return !Resolve().EnvironmentVariablesOverridden; } }
        [PythonAPI("The status of the last run: None, InQueue, Running, OK, Killed, TimedOut, Failed or " +
                   "FailedWithResults.", Returns = "str")]
        public string status { get { return Resolve().JobStatus.ToString(); } }
        [PythonAPI("The name of the results file the analysis writes.", Returns = "str")]
        public string results_file_name { get { return _service.GetResultsFileName(Resolve()); } }
        [PythonAPI("True when the analysis results match the current model.", Returns = "bool")]
        public bool is_up_to_date { get { return _service.IsUpToDate(Resolve()); } }


        // Constructors
        internal JobAPI(string jobName, JobService service)
        {
            _jobName = jobName;
            _service = service;
        }


        // Methods
        [PythonAPI("Writes the solver input file and runs the analysis using command history. It returns " +
                   "the job status, which is OK when the solver finished successfully. With wait the call " +
                   "returns only when the solver has finished; a timeout in seconds kills the solver and " +
                   "returns TimedOut. The timeout starts when solver submission begins; refused preparation raises an error.",
                   Arguments = "wait: bool = True, timeout: float = None", Returns = "str",
                   Example = "print(job.run(timeout=600))")]
        public string run(object wait = null, object timeout = null)
        {
            return _service.Run(_jobName, false, wait, timeout);
        }

        [PythonAPI("Writes the solver input file and only checks the model, without solving it. It returns " +
                   "the job status.", Arguments = "wait: bool = True, timeout: float = None",
                   Returns = "str")]
        public string check_model(object wait = null, object timeout = null)
        {
            return _service.Run(_jobName, true, wait, timeout);
        }

        [PythonAPI("Kills a running analysis. An analysis that is not running is refused, as the user interface " +
                   "refuses it, and so is any kill in read only automation mode.", Returns = "None")]
        public void kill()
        {
            _service.Kill(_jobName);
        }

        [PythonAPI("Returns everything the solver wrote to its output.", Returns = "str")]
        public string get_output()
        {
            return Resolve().GetAllOutputData();
        }
        public override string ToString()
        {
            AnalysisJob job = Resolve();
            return "Job(name=" + StringListAPI.ToDisplayString(job.Name) + ", solver=" +
                   job.FEMSolver + ", status=" + job.JobStatus + ")";
        }
        //
        private AnalysisJob Resolve()
        {
            return _service.ResolveJob(_jobName);
        }
    }
}
