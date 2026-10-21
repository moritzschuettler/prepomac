// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Management;
using System.ComponentModel;
using CaeGlobals;
using System.Runtime.Serialization;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CaeJob
{
    [Serializable]
    public enum JobStatus
    {
        None,
        InQueue,
        Running,
        OK,
        Killed,
        TimedOut,
        Failed,
        FailedWithResults,
    }
    //
    [Serializable]
    public enum FEMSolverEnum
    {
        Calculix,
        Abaqus,
        FEMaster,
        OpenRadioss
    }
    //
    [Serializable]
    public class AnalysisJob : NamedClass, ISerializable
    {
        // Variables                                                                                                                        
        protected string _workDirectory;                                // ISerializable
        protected string _executable;                                   // ISerializable
        protected string _argument;                                     // ISerializable
        protected bool _compatibilityMode;                              // ISerializable
        protected JobStatus _jobStatus;                                 // ISerializable
        protected int _numCPUs;                                         // ISerializable
        protected List<EnvironmentVariable> _environmentVariables;      // ISerializable
        protected long _datFileLength;                                  // ISerializable
        protected string _datFileContents;                              // ISerializable
        protected long _statusFileLength;                               // ISerializable
        protected string _statusFileContents;                           // ISerializable
        protected long _convergenceFileLength;                          // ISerializable
        protected string _convergenceFileContents;                      // ISerializable
        protected DateTime _endTime;                                    // ISerializable
        protected FEMSolverEnum _femSolver;                             // ISerializable
        protected bool _onlyCheckModel;                                 // ISerializable
        protected bool _executableOverridden;                           // ISerializable
        protected bool _workDirectoryOverridden;                       // ISerializable
        //
        [NonSerialized] protected Stopwatch _watch;
        [NonSerialized] private Process _exe;
        [NonSerialized] private StringBuilder _sbOutput;
        [NonSerialized] private StringBuilder _sbAllOutput;
        [NonSerialized] private bool _datFileToLong;
        [NonSerialized] private string _outputFileName;
        [NonSerialized] private string _errorFileName;
        [NonSerialized] private object _myLock;
        [NonSerialized] private string _inputFileName;
        [NonSerialized] private int _numOfRunSteps;
        [NonSerialized] private int _currentRunStep;
        [NonSerialized] private int _numOfRunIncrements;
        [NonSerialized] private int _currentRunIncrement;
        [NonSerialized] private object _tag;
        [NonSerialized] private bool _useBackgroundWorker;
        [NonSerialized] private double _prevWatchMilliseconds;
        [NonSerialized] private CalculixEnergyOutputParser _energyOutputParser;


        [NonSerialized] private int _runTimeoutMilliseconds;
        [NonSerialized] private Stopwatch _runTimeoutWatch;
        private bool _numCPUsOverridden;
        private bool _environmentVariablesOverridden;


        // Properties                                                                                                               
        public override string Name
        {
            get { return base.Name; }
            set
            {
                if (base.Name != value)
                {
                    base.Name = value;
                    //
                    _argument = GetDefaultArgument(_femSolver, Name);
                }
            }
        }
        public string WorkDirectory
        {
            get { return _workDirectory; }
            set { _workDirectory = value; }
        }
        public string Executable
        {
            get { return _executable; }
            set { _executable = value; }
        }
        public string Argument
        {
            get { return _argument; }
            set { _argument = value; }
        }
        public string ResultsFileName
        {
            get
            {
                if (_femSolver == FEMSolverEnum.Calculix) return Path.Combine(WorkDirectory, _name + ".frd");
                else if (_femSolver == FEMSolverEnum.Abaqus) return Path.Combine(WorkDirectory, _name + ".odb");
                else if (_femSolver == FEMSolverEnum.FEMaster) return Path.Combine(WorkDirectory, _name + ".frd");
                else if (_femSolver == FEMSolverEnum.OpenRadioss) return Path.Combine(WorkDirectory, _name + "A001");
                else throw new NotSupportedException();
            }
        }
        public bool IsUpToDate
        {
            get
            {
                // A missing file reads as written in 1601, which every finished run would be newer than
                if (!File.Exists(ResultsFileName)) return false;
                DateTime lastWriteTime = File.GetLastWriteTime(ResultsFileName);
                return _endTime > lastWriteTime;
            }
        }
        public bool CompatibilityMode { get { return _compatibilityMode; } set { _compatibilityMode = value; } }
        public JobStatus JobStatus { get { return _jobStatus; } }
        public int NumCPUs
        {
            get { return _numCPUs; }
            set
            {
                _numCPUs = value;

                ProcessStartInfo psi = new ProcessStartInfo();
                SetNumberOfProcessors(psi);
            }
        }
        public List<EnvironmentVariable> EnvironmentVariables
        {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }
        public string DatFileData { get { return _datFileContents; } }
        public string StatusFileData { get { return _statusFileContents; } }
        public string ConvergenceFileData { get { return _convergenceFileContents; } }
        public int CurrentRunStep { get { return _currentRunStep; } }
        public int CurrentRunIncrement { get { return _currentRunIncrement; } }
        public string InputFileName { get { return _inputFileName; } }
        public FEMSolverEnum FEMSolver
        {
            get { return _femSolver; }
            set
            {
                if (_femSolver != value)
                {
                    _femSolver = value;
                    //
                    _argument = GetDefaultArgument(_femSolver, Name);
                }
            }
        }
        public bool OnlyCheckModel { get { return _onlyCheckModel; } set { _onlyCheckModel = value; } }
        public bool ExecutableOverridden { get { return _executableOverridden; } set { _executableOverridden = value; } }
        public bool WorkDirectoryOverridden { get { return _workDirectoryOverridden; } set { _workDirectoryOverridden = value; } }
        public bool NumCPUsOverridden { get { return _numCPUsOverridden; } set { _numCPUsOverridden = value; } }
        public bool EnvironmentVariablesOverridden
        {
            get { return _environmentVariablesOverridden; }
            set { _environmentVariablesOverridden = value; }
        }
        public object Tag { get { return _tag; } set { _tag = value; } }


        // Events                                                                                                                   
        public event Action<AnalysisJob, string> DataOutputEvent;


        // Callback                                                                                                                 
        public Action<string, JobStatus> JobStatusChanged;
        public Action<AnalysisJob> PreRun;
        public Action<AnalysisJob> PostRun;
        public Action<AnalysisJob> LastRunCompleted;


        // Constructor                                                                                                              
        public AnalysisJob(string name, string executable, string argument, string workDirectory)
            : base(name)
        {
            _executable = executable;
            _argument = argument;
            _workDirectory = workDirectory;
            _compatibilityMode = false;
            _numCPUs = 1;
            _environmentVariables = new List<EnvironmentVariable>();
            _endTime = DateTime.MinValue;
            //
            _exe = null;
            _jobStatus = JobStatus.None;
            _watch = null;
            _sbOutput = null;
            _sbAllOutput = null;
            _energyOutputParser = new CalculixEnergyOutputParser();
            _onlyCheckModel = false;
            _executableOverridden = false;
            _workDirectoryOverridden = false;
        }
        public AnalysisJob(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Compatibility v 2.3.5
            _executableOverridden = false;
            _workDirectoryOverridden = false;
            _femSolver = FEMSolverEnum.Calculix;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_workDirectory":
                        _workDirectory = (string)entry.Value; break;
                    case "_executable":
                        _executable = (string)entry.Value; break;
                    case "_argument":
                        _argument = (string)entry.Value; break;
                    case "_compatibilityMode":
                        _compatibilityMode = (bool)entry.Value; break;
                    case "_jobStatus":
                        _jobStatus = (JobStatus)entry.Value; break;
                    case "_numCPUs":
                        _numCPUs = (int)entry.Value; break;
                    case "_environmentVariables":
                        _environmentVariables = (List<EnvironmentVariable>)entry.Value; break;
                    case "_datFileLength":
                        _datFileLength = (long)entry.Value; break;
                    case "_datFileContents":
                        _datFileContents = (string)entry.Value; break;
                    case "_statusFileLength":
                        _statusFileLength = (long)entry.Value; break;
                    case "_statusFileContents":
                        _statusFileContents = (string)entry.Value; break;
                    case "_convergenceFileLength":
                        _convergenceFileLength = (long)entry.Value; break;
                    case "_convergenceFileContents":
                        _convergenceFileContents = (string)entry.Value; break;
                    case "_endTime":
                        _endTime = (DateTime)entry.Value; break;
                    case "_femSolver":
                        _femSolver = (FEMSolverEnum)entry.Value; break;
                    case "_onlyCheckModel":
                        _onlyCheckModel = (bool)entry.Value; break;
                    case "_executableOverridden":
                        _executableOverridden = (bool)entry.Value; break;
                    case "_workDirectoryOverridden":
                        _workDirectoryOverridden = (bool)entry.Value; break;
                    case "_numCPUsOverridden":
                        _numCPUsOverridden = (bool)entry.Value; break;
                    case "_environmentVariablesOverridden":
                        _environmentVariablesOverridden = (bool)entry.Value; break;
                    default:
                        break;
                }
            }
            _energyOutputParser = new CalculixEnergyOutputParser();
        }

        // Event handlers                                                                                                           
        void Timer_Tick(object sender, EventArgs e)
        {
            //OutputData();
        }


        // Methods                                                                                                                  
        public void Submit(int numOfRunSteps, int numOfRunIncrements, bool useBackgroundWorker = true,
                           int timeoutMilliseconds = 0)
        {
            if (numOfRunSteps < 1 || numOfRunIncrements < 1) throw new NotSupportedException();
            if (timeoutMilliseconds < 0) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            _runTimeoutMilliseconds = timeoutMilliseconds;
            _runTimeoutWatch = Stopwatch.StartNew();
            _useBackgroundWorker = useBackgroundWorker;
            // Reset job
            _tag = null;
            _jobStatus = JobStatus.None;
            //
            if (_femSolver == FEMSolverEnum.OpenRadioss)
                _inputFileName = Path.Combine(_workDirectory, _name + "_0000.rad");  // must be first for the timer to work
            else
                _inputFileName = Path.Combine(_workDirectory, _name + ".inp");  // must be first for the timer to work
            //
            _numOfRunSteps = numOfRunSteps;
            _currentRunStep = -1;
            //
            _numOfRunIncrements = numOfRunIncrements;
            _currentRunIncrement = -1;
            //
            _watch = new Stopwatch();
            _watch.Start();
            _prevWatchMilliseconds = 0;
            //
            SubmitNextRun();
        }
        private void SubmitNextRun()
        {
            // First run
            if (_currentRunStep == -1)
            {
                _currentRunStep = 1;
                _currentRunIncrement = 1;
            }
            // Other runs
            else
            {
                _currentRunIncrement++;
                //
                if (_currentRunIncrement > _numOfRunIncrements)
                {
                    _currentRunIncrement = 1;
                    _currentRunStep++;
                }
            }
            //
            if (_currentRunStep <= _numOfRunSteps && (_jobStatus == JobStatus.None || _jobStatus == JobStatus.OK))
            {
                PreRun?.Invoke(this);
                SubmitOneRun();
            }
            else AllRunsCompleted();
        }
        private void SubmitOneRun()
        {
            if (_myLock == null) _myLock = new object();
            lock (_myLock)
            {
                if (_sbOutput == null) _sbOutput = new StringBuilder();
                _sbOutput.Clear();
                //
                if (_sbAllOutput == null) _sbAllOutput = new StringBuilder();
                _sbAllOutput.Clear();
                //
                if (_energyOutputParser == null) _energyOutputParser = new CalculixEnergyOutputParser();
                else _energyOutputParser.Reset();
            }
            //
            AppendDataToOutput(DateTime.Now + Environment.NewLine);
            AppendDataToOutput("########   Starting run step number: " + _currentRunStep +
                               "   Increment number: "  + _currentRunIncrement + "   ########" +  Environment.NewLine);
            AppendDataToOutput("Running command: " + _executable + " " + GetArgument());
            //
            _datFileLength = -1;
            _datFileToLong = false;
            _datFileContents = "";
            //
            _statusFileLength = -1;
            _statusFileContents = "";
            //
            _convergenceFileLength = -1;
            _convergenceFileContents = "";
            //
            _jobStatus = JobStatus.Running;
            //
            JobStatusChanged?.Invoke(_name, _jobStatus);
            //
            if (_useBackgroundWorker)
            {
                using (BackgroundWorker bwStart = new BackgroundWorker())
                {
                    bwStart.DoWork += bwStart_DoWork;
                    bwStart.RunWorkerCompleted += bwStart_RunWorkerCompleted;
                    if (!bwStart.IsBusy) bwStart.RunWorkerAsync();
                }
            }
            else
            {
                bwStart_DoWork(null, null);
                bwStart_RunWorkerCompleted(null, null);
            }
        }
        private void bwStart_DoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = Path.GetFileName(Name);
            _outputFileName = Path.Combine(_workDirectory, "_output_" + fileName + ".txt");
            _errorFileName = Path.Combine(_workDirectory, "_error_" + fileName + ".txt");
            //
            if (File.Exists(_outputFileName)) File.Delete(_outputFileName);
            if (File.Exists(_errorFileName)) File.Delete(_errorFileName);
            //
            if (_femSolver == FEMSolverEnum.OpenRadioss) Run_OpenRadioss();
            else if (Tools.IsWindows10orNewer() && !_compatibilityMode) Run_Win10(_executable, GetArgument());
            else Run_OldWin(_executable, GetArgument());
        }
        private void Run_OpenRadioss()
        {
            string starterExe = _executable;
            string engineExe = GetOpenRadiossEngineExe(starterExe);
            if (!File.Exists(engineExe))
            {
                AppendDataToOutput(" The OpenRadioss engine executable was not found: " + engineExe);
                _jobStatus = JobStatus.Failed;
                return;
            }
            // Starter
            AppendDataToOutput("");
            AppendDataToOutput("########   OpenRadioss starter   ########");
            RunOneExecutable(starterExe, "-i " + _name + "_0000.rad -np 1 -nt " + _numCPUs);
            if (_jobStatus != JobStatus.OK) return;
            if (ContainsError(_sbOutput.ToString()) || ContainsError(_sbAllOutput.ToString()))
            {
                _jobStatus = JobStatus.Failed;
                return;
            }
            // Engine: run all <name>_0001.rad, <name>_0002.rad, ... files in order
            string[] engineFileNames = GetOpenRadiossEngineFileNames();
            if (engineFileNames.Length == 0)
            {
                AppendDataToOutput(" No OpenRadioss engine file (" + _name + "_0001.rad) was found.");
                _jobStatus = JobStatus.Failed;
                return;
            }
            foreach (string engineFileName in engineFileNames)
            {
                AppendDataToOutput("");
                AppendDataToOutput("########   OpenRadioss engine: " + Path.GetFileName(engineFileName) + "   ########");
                RunOneExecutable(engineExe, "-i " + Path.GetFileName(engineFileName) + " -nt " + _numCPUs);
                if (_jobStatus != JobStatus.OK) return;
            }
        }
        private void RunOneExecutable(string executable, string argument)
        {
            if (Tools.IsWindows10orNewer() && !_compatibilityMode) Run_Win10(executable, argument);
            else Run_OldWin(executable, argument);
        }
        public static string GetOpenRadiossEngineExe(string starterExe)
        {
            string fileName = Path.GetFileName(starterExe);
            string engineFileName = fileName.Replace("starter", "engine");
            return Path.Combine(Path.GetDirectoryName(starterExe), engineFileName);
        }
        private string[] GetOpenRadiossEngineFileNames()
        {
            List<string> engineFileNames = new List<string>();
            int runNumber = 1;
            while (true)
            {
                string engineFileName = Path.Combine(_workDirectory, _name + "_" + runNumber.ToString().PadLeft(4, '0') + ".rad");
                if (File.Exists(engineFileName)) engineFileNames.Add(engineFileName);
                else break;
                runNumber++;
            }
            return engineFileNames.ToArray();
        }
        private void bwStart_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool resultsExist = false;
            if (_femSolver == FEMSolverEnum.Calculix)
            {
                string resultsFileName = ResultsFileName;
                if (File.Exists(resultsFileName))
                {
                    long length = new FileInfo(resultsFileName).Length;
                    if (length > 15 * 20) resultsExist = true;
                }
            }
            else if (_femSolver == FEMSolverEnum.Abaqus)
            {
                string odbFailFileName = Path.Combine(WorkDirectory, _name + ".odb_f");
                if (!File.Exists(odbFailFileName)) resultsExist = true;
            }
            else if (_femSolver == FEMSolverEnum.FEMaster)
            {
                string resultsFileName = ResultsFileName;
                if (File.Exists(resultsFileName))
                {
                    long length = new FileInfo(resultsFileName).Length;
                    if (length > 15 * 20) resultsExist = true;
                }
            }
            else if (_femSolver == FEMSolverEnum.OpenRadioss)
            {
                if (File.Exists(ResultsFileName)) resultsExist = true;
            }
            else throw new NotSupportedException();
            //
            bool continueAnalysis = false;
            AppendDataToOutput("");
            if (_jobStatus == JobStatus.OK)
            {
                if (!resultsExist && !(ContainsNoAnalysis(_sbOutput.ToString()) || ContainsNoAnalysis(_sbAllOutput.ToString())))
                {
                    AppendDataToOutput(" Job failed - no results exist.");
                    _jobStatus = JobStatus.Failed;
                }
                else if (ContainsError(_sbOutput.ToString()) || ContainsError(_sbAllOutput.ToString()))
                {
                    _jobStatus = JobStatus.FailedWithResults;
                }
                else
                {
                    PostRun?.Invoke(this);
                    continueAnalysis = true;                    
                }
            }
            else if (_jobStatus == JobStatus.Killed)
            {
                AppendDataToOutput(" Job killed.");
            }
            else if (_jobStatus == JobStatus.Failed)
            {
                AppendDataToOutput(" Job failed.");
                if (resultsExist) _jobStatus = JobStatus.FailedWithResults;
            }
            //
            if (continueAnalysis) SubmitNextRun();
            else AllRunsCompleted();
        }
        private bool ContainsError(string text)
        {
            if (_femSolver == FEMSolverEnum.Calculix)
            {
                // An error that does not stop the run, such as a temperature file that cannot be read, has no asterisk
                if (text.Contains(" ERROR in ") || text.StartsWith("ERROR in ") || text.Contains("\nERROR in "))
                    return true;
                //*ERROR reading *DYNAMIC: initial increment size exce eds step size
                if (text.Contains("*ERROR"))
                {
                    if (text.Contains("*ERROR reading *DYNAMIC: initial increment size"))
                        return text.AllIndicesOf("*ERROR").Count() > 1;
                    else return true;
                }
            }
            else if (_femSolver == FEMSolverEnum.Abaqus)
            {
                if (text.Contains("Abaqus/Analysis exited with errors")) return true;
            }
            else if (_femSolver == FEMSolverEnum.FEMaster)
            {
            }
            else if (_femSolver == FEMSolverEnum.OpenRadioss)
            {
                if (text.Contains("ERROR ID :") || text.Contains("ERROR TERMINATION")) return true;
            }
            else throw new NotSupportedException();
            //
            return false;
        }
        private bool ContainsNoAnalysis(string text)
        {
            //*ERROR reading *DYNAMIC: initial increment size exceeds step size
            if (text.Contains("*WARNING: no analysis option was chosen")) return true;
            else return false;
        }
        private void AllRunsCompleted()
        {
            _watch.Stop();
            //
            AppendDataToOutput("");
            AppendDataToOutput("Process elapsed time:       " + Math.Round(_watch.Elapsed.TotalSeconds, 3).ToString() + " s");
            //AppendDataToOutput(" Peak physical memory usage: " + (Math.Round(_peakWorkingSet / 1048576.0, 3)).ToString() + " MB");
            //AppendDataToOutput(" Peak paged memory usage:    " + (Math.Round(_peakPagedMem / 1048576.0, 3)).ToString() + " MB");
            //Console.WriteLine($"  Peak virtual memory usage  : {_peakVirtualMem / 1024 / 1024}");
            //
            Timer_Tick(null, null);
            if (_energyOutputParser != null) _energyOutputParser.Complete();
            SendDataToOutput();
            //
            JobStatusChanged?.Invoke(_name, _jobStatus);
            LastRunCompleted?.Invoke(this);
            //
            _endTime = DateTime.Now + TimeSpan.FromSeconds(1);
            // Dereference the links to other objects
            DataOutputEvent = null;
            JobStatusChanged = null;
            PreRun = null;
            PostRun = null;
            LastRunCompleted = null;
        }
        private void Run_Test()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = @"C:\Temp\test.bat";
            ////psi.Arguments = _argument;
            //psi.WorkingDirectory = _workDirectory;
            //psi.UseShellExecute = false;
            ////
            ////SetEnvironmentVariables(psi);
            ////
            _exe = new Process();
            _exe.StartInfo = psi;
            _exe.Start();
            ////
            int ms = GetRemainingRunTime();
            if (_exe.WaitForInputIdle(ms))
            {
                // Process completed. Check process.ExitCode here.
                // after Kill() _jobStatus is Killed
                // A kill or a time out has already decided the outcome and must not be overwritten here
                if (_jobStatus != JobStatus.Killed && _jobStatus != JobStatus.TimedOut)
                    _jobStatus = JobStatus.OK;
            }
            else
            {
                // Timed out.
                Kill("Time out.");
                //Debug.WriteLine(DateTime.Now + "   Timeout proces: " + Name + " in: " + _workDirectory);
                _jobStatus = JobStatus.TimedOut;
            }
            _exe.Close();
        }
        private void Run_OldWin(string executable, string argument)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = executable;
            psi.Arguments = argument;
            psi.WorkingDirectory = _workDirectory;
            psi.UseShellExecute = false;
            //
            SetEnvironmentVariables(psi);
            //
            _exe = new Process();
            _exe.StartInfo = psi;
            _exe.Start();
            ChildProcessJob.Add(_exe);
            //
            int ms = GetRemainingRunTime();
            if (_exe.WaitForExit(ms))
            {
                // Process completed. Check process.ExitCode here.

                // after Kill() _jobStatus is Killed
                // A kill or a time out has already decided the outcome and must not be overwritten here
                if (_jobStatus != JobStatus.Killed && _jobStatus != JobStatus.TimedOut)
                    _jobStatus = GetCompletedStatus(_exe.ExitCode);
            }
            else
            {
                // Timed out.
                Kill("Time out.");
                //Debug.WriteLine(DateTime.Now + "   Timeout proces: " + Name + " in: " + _workDirectory);
                _jobStatus = JobStatus.TimedOut;
            }
            _exe.Close();
        }
        // A solver that crashed - a segmentation fault ends CalculiX without an error message and without the
        // steps after it - exits with a code other than zero, as one that stopped on an error does. A model check,
        // which runs the steps without an analysis, ends with an exit code other than zero as well
        private JobStatus GetCompletedStatus(int exitCode)
        {
            if (exitCode == 0) return JobStatus.OK;
            if (ContainsNoAnalysis(_sbOutput.ToString()) || ContainsNoAnalysis(_sbAllOutput.ToString())) return JobStatus.OK;
            //
            AppendDataToOutput(" The solver ended with the exit code " + exitCode + ".");
            return JobStatus.Failed;
        }
        private void Run_Win10(string executable, string argument)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.FileName = executable;
            //
            psi.Arguments = argument;
            psi.WorkingDirectory = _workDirectory;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            //
            SetEnvironmentVariables(psi);
            //
            _exe = new Process();
            _exe.StartInfo = psi;            
            //
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                _exe.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        // the safe wait handle closes on kill
                        if (!outputWaitHandle.SafeWaitHandle.IsClosed) outputWaitHandle.Set();
                    }
                    else
                    {
                        AppendDataToOutput(e.Data);
                    }
                };
                //
                _exe.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        // the safe wait handle closes on kill
                        if (!errorWaitHandle.SafeWaitHandle.IsClosed) errorWaitHandle.Set();
                    }
                    else
                    {
                        File.AppendAllText(_errorFileName, e.Data + Environment.NewLine);
                        AppendDataToOutput(e.Data);
                    }
                };
                //
                _exe.Start();
                ChildProcessJob.Add(_exe);
                //
                _exe.BeginOutputReadLine();
                _exe.BeginErrorReadLine();
                int ms = GetRemainingRunTime();
                if (_exe.WaitForExit(ms) && outputWaitHandle.WaitOne(GetRemainingRunTime()) &&
                    errorWaitHandle.WaitOne(GetRemainingRunTime()))
                {
                    // Process completed. Check process.ExitCode here.
                    // after Kill() _jobStatus is Killed
                    // A kill or a time out has already decided the outcome and must not be overwritten here
                    if (_jobStatus != JobStatus.Killed && _jobStatus != JobStatus.TimedOut)
                        _jobStatus = GetCompletedStatus(_exe.ExitCode);
                }
                else
                {
                    // Timed out.
                    Kill("Time out.");
                    //Debug.WriteLine(DateTime.Now + "   Timeout process: " + Name + " in: " + _workDirectory);
                    _jobStatus = JobStatus.TimedOut;
                }               
                _exe.Close();
                
                // Abaqus
                //if (_femSolver == FEMSolverEnum.Abaqus)
                //{
                //    string fileName = Path.Combine(_workDirectory, Name + ".dat");
                //    if (File.Exists(fileName))
                //    {
                //        AppendDataToOutput(Environment.NewLine + 
                //                           "########   ABAQUS .dat FILE CONTENT   ########" +
                //                           Environment.NewLine);
                //        AppendDataToOutput(File.ReadAllText(fileName));
                //    }
                //}
            }            
        }
        private int GetRemainingRunTime()
        {
            if (_runTimeoutMilliseconds == 0) return 1000 * 3600 * 24 * 7 * 3;
            long remaining = _runTimeoutMilliseconds - _runTimeoutWatch.ElapsedMilliseconds;
            return remaining > 0 ? (int)remaining : 1;
        }
        public void Kill(string message)
        {
            try
            {
                if (_exe != null)
                {
                    if (message != "The string builder run out of space.") AppendDataToOutput(message);
                    //
                    _watch.Stop();
                    //
                    try
                    {
                        UInt32 id = (UInt32)_exe.Id;
                        KillAllProcessesSpawnedBy(id);
                        if (!_exe.HasExited) _exe.Kill();    // must be here
                    }
                    catch { }
                }
            }
            finally
            {
                _jobStatus = JobStatus.Killed;
            }
        }
        private static void KillChildProcessesUnix(UInt32 parentProcessId)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("pgrep", "-P " + parentProcessId);
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                string output;
                using (Process pgrep = Process.Start(psi))
                {
                    output = pgrep.StandardOutput.ReadToEnd();
                    pgrep.WaitForExit();
                }
                foreach (var line in output.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out int childProcessId) && childProcessId != Process.GetCurrentProcess().Id)
                    {
                        KillChildProcessesUnix((UInt32)childProcessId);
                        try { Process.GetProcessById(childProcessId).Kill(); }
                        catch { }
                    }
                }
            }
            catch
            {
            }
        }
        public static void KillAllProcessesSpawnedBy(UInt32 parentProcessId)
        {
            // NOTE: Process Ids are reused!
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                // No WMI outside Windows: the child processes are found with pgrep
                KillChildProcessesUnix(parentProcessId);
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parentProcessId);
            ManagementObjectCollection collection = searcher.Get();
            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    UInt32 childProcessId = (UInt32)item["ProcessId"];
                    if ((int)childProcessId != Process.GetCurrentProcess().Id)
                    {
                        KillAllProcessesSpawnedBy(childProcessId);
                        //
                        try
                        {
                            Process childProcess = Process.GetProcessById((int)childProcessId);
                            childProcess.Kill();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        //
        public string GetAllOutputData()
        {
            try
            {
                if (_sbAllOutput != null) return _sbAllOutput.ToString();
                else return null;
            }
            catch
            {
                return null;
            }
        }
        public CalculixEnergyData GetEnergyData()
        {
            if (_myLock == null) _myLock = new object();
            lock (_myLock)
            {
                if (_energyOutputParser == null) _energyOutputParser = new CalculixEnergyOutputParser();
                return _energyOutputParser.GetData();
            }
        }
        private void SendDataToOutput()
        {
            if (_myLock == null) _myLock = new object();
            lock (_myLock)
            {
                string outputData = _sbOutput.ToString();
                //
                if (_outputFileName != null)
                {
                    try
                    {
                        File.AppendAllText(_outputFileName, outputData);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        // The work directory was removed while the job ran: the output stays in memory only
                        _outputFileName = null;
                    }
                }
                //
                GetDatFileContents();
                GetStatusFileContents();
                GetConvergenceFileContents();
                //
                DataOutputEvent?.Invoke(this, outputData);
                //
                if (_sbAllOutput.Length > 2_000_000_000) Kill("The string builder ran out of space.");
                //
                _sbAllOutput.Append(_sbOutput);
                _sbOutput.Clear();
            }
        }
        private void AppendDataToOutput(string data)
        {
            if (_myLock == null) _myLock = new object();
            lock (_myLock)
            {
                Application.DoEvents();
                _sbOutput.AppendLine(data);
                if (_femSolver == FEMSolverEnum.Calculix)
                {
                    if (_energyOutputParser == null) _energyOutputParser = new CalculixEnergyOutputParser();
                    _energyOutputParser.AppendLine(data);
                }
            }
            //
            if (_watch.ElapsedMilliseconds - _prevWatchMilliseconds > 1000)
            {
                SendDataToOutput();
                _prevWatchMilliseconds = _watch.ElapsedMilliseconds;
            }
        }
        private void GetDatFileContents()
        {
            try
            {
                if (!_datFileToLong)
                {
                    string datFileName = Path.Combine(_workDirectory, Name + ".dat");
                    if (!File.Exists(datFileName)) return;
                    //
                    long size = new FileInfo(datFileName).Length;
                    //
                    if (size != _datFileLength)
                    {
                        using (FileStream fileStream = new FileStream(datFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (StreamReader streamReader = new StreamReader(fileStream))
                            {
                                _datFileContents = streamReader.ReadToEnd();
                            }
                        }
                        _datFileLength = size;
                    }
                    //
                    int sizeLimit = 100_000;
                    if (_datFileLength > sizeLimit)
                    {
                        if (!_datFileToLong)    // first time
                        {
                            _datFileContents = _datFileContents.Substring(0, sizeLimit);
                            _datFileContents += Environment.NewLine + Environment.NewLine +
                                                "Warning: .dat file is too long to be displayed in the Monitor form.";
                        }
                        _datFileToLong = true;
                    }
                }
            }
            catch
            {
            }
        }
        private void GetStatusFileContents()
        {
            try
            {
                string statusFileName = Path.Combine(_workDirectory, Name + ".sta");
                if (!File.Exists(statusFileName)) return;
                //
                long size = new FileInfo(statusFileName).Length;
                //
                if (size != _statusFileLength)
                {
                    using (FileStream fileStream = new FileStream(statusFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader streamReader = new StreamReader(fileStream))
                        {
                            _statusFileContents = streamReader.ReadToEnd();
                        }
                    }
                    _statusFileLength = size;
                }
            }
            catch
            {
            }
        }
        private void GetConvergenceFileContents()
        {
            try
            {
                string convergenceFileName = Path.Combine(_workDirectory, Name + ".cvg");
                if (!File.Exists(convergenceFileName)) return;
                //
                long size = new FileInfo(convergenceFileName).Length;
                //
                if (size != _convergenceFileLength)
                {
                    using (FileStream fileStream = new FileStream(convergenceFileName, FileMode.Open, FileAccess.Read,
                                                                  FileShare.ReadWrite))
                    {
                        using (StreamReader streamReader = new StreamReader(fileStream))
                        {
                            _convergenceFileContents = streamReader.ReadToEnd();
                        }
                    }
                    _convergenceFileLength = size;
                }
            }
            catch
            {
            }
        }
        //
        public void ClearFileContents()
        {
            _datFileLength = 0;
            _datFileContents = "";
            _statusFileLength = 0;
            _statusFileContents = "";
            _convergenceFileLength = 0;
            _convergenceFileContents = "";
            if (_energyOutputParser != null) _energyOutputParser.Reset();
        }
        //
        public static string GetDefaultArgument(FEMSolverEnum femSolver, string name)
        {
            if (femSolver == FEMSolverEnum.Calculix) return name;
            else if (femSolver == FEMSolverEnum.Abaqus) return "job=" + name;
            else if (femSolver == FEMSolverEnum.FEMaster) return name + ".inp";
            else if (femSolver == FEMSolverEnum.OpenRadioss) return "-i " + name + "_0000.rad";
            else throw new NotSupportedException();
        }
        public static bool IsAdministrator()
        {
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            // DOMAINNAME\Domain Admins RID: 0x200
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) || principal.IsInRole(0x200);
        }
        public void ResetJobStatus()
        {
            _jobStatus = JobStatus.None;
        }
        //
        private void SetEnvironmentVariables(ProcessStartInfo psi)
        {
            SetNumberOfProcessors(psi);
            if (_femSolver == FEMSolverEnum.OpenRadioss) SetOpenRadiossEnvironmentVariables(psi);
            if (_environmentVariables != null)
            {
                foreach (var environmentVariable in _environmentVariables)
                {
                    SetEnvironmentVariable(psi, environmentVariable);
                }
            }
        }
        private void SetOpenRadiossEnvironmentVariables(ProcessStartInfo psi)
        {
            // Folder structure: <root>\exec\starter_win64.exe, <root>\hm_cfg_files, <root>\extlib\...
            string execDirectory = Path.GetDirectoryName(_executable);
            string rootDirectory = Path.GetDirectoryName(execDirectory);
            if (rootDirectory == null) return;
            //
            string cfgPath = Path.Combine(rootDirectory, "hm_cfg_files");
            if (Directory.Exists(cfgPath)) SetEnvironmentVariable(psi, new EnvironmentVariable("RAD_CFG_PATH", cfgPath));
            //
            string h3dPath = Path.Combine(rootDirectory, "extlib", "h3d", "lib", "win64");
            if (Directory.Exists(h3dPath)) SetEnvironmentVariable(psi, new EnvironmentVariable("RAD_H3D_PATH", h3dPath));
            //
            string hmReaderPath = Path.Combine(rootDirectory, "extlib", "hm_reader", "win64");
            string intelRuntimePath = Path.Combine(rootDirectory, "extlib", "intelOneAPI_runtime", "win64");
            string path = execDirectory;
            if (Directory.Exists(hmReaderPath)) path += ";" + hmReaderPath;
            if (Directory.Exists(h3dPath)) path += ";" + h3dPath;
            if (Directory.Exists(intelRuntimePath)) path += ";" + intelRuntimePath;
            path += ";" + Environment.GetEnvironmentVariable("PATH");
            if (psi.EnvironmentVariables.ContainsKey("PATH")) psi.EnvironmentVariables.Remove("PATH");
            psi.EnvironmentVariables.Add("PATH", path);
        }
        private void SetNumberOfProcessors(ProcessStartInfo psi)
        {
            EnvironmentVariable numberOfProcessors = new EnvironmentVariable("OMP_NUM_THREADS", _numCPUs.ToString());
            SetEnvironmentVariable(psi, numberOfProcessors);
            // The multithreaded SPOOLES solver of the CalculiX builds for macOS and Linux returns wrong results;
            // an environment variable of the job can still set it
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                SetEnvironmentVariable(psi, new EnvironmentVariable("CCX_NPROC_EQUATION_SOLVER", "1"));
        }
        private void SetEnvironmentVariable(ProcessStartInfo psi, EnvironmentVariable environmentVariable)
        {
            try
            {
                if (environmentVariable.Active)
                {
                    if (psi.EnvironmentVariables.ContainsKey(environmentVariable.Name)) psi.EnvironmentVariables.Remove(environmentVariable.Name);
                    psi.EnvironmentVariables.Add(environmentVariable.Name, environmentVariable.Value);
                    if (!psi.EnvironmentVariables.ContainsKey(environmentVariable.Name)) throw new Exception();
                }
            }
            catch
            {
                AppendDataToOutput("To add environmental variable '" + environmentVariable.Name + "' to the analysis the program must be run with elevated permisions (Run as administrator).");
            }
        }
        private string GetArgument()
        {
            string argument = "";
            if (_femSolver == FEMSolverEnum.Calculix) argument = _argument;
            else if (_femSolver == FEMSolverEnum.Abaqus)
            {
                if (_onlyCheckModel) argument = "datacheck ";
                argument += "interactive ask_delete=OFF ";
                if (_numCPUs > 1) argument += "cpus=" + _numCPUs + " ";
                argument += _argument;
            }
            else if (_femSolver == FEMSolverEnum.FEMaster)
            {
                argument = _argument + " --ncpus " + _numCPUs;
            }
            else if (_femSolver == FEMSolverEnum.OpenRadioss)
            {
                argument = "-i " + _name + "_0000.rad -np 1 -nt " + _numCPUs;
            }
            else throw new NotSupportedException();
            //
            return argument;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_workDirectory", _workDirectory, typeof(string));
            info.AddValue("_executable", _executable, typeof(string));
            info.AddValue("_argument", _argument, typeof(string));
            info.AddValue("_compatibilityMode", _compatibilityMode, typeof(bool));
            info.AddValue("_jobStatus", _jobStatus, typeof(JobStatus));
            info.AddValue("_numCPUs", _numCPUs, typeof(int));
            info.AddValue("_environmentVariables", _environmentVariables, typeof(List<EnvironmentVariable>));
            info.AddValue("_datFileLength", _datFileLength, typeof(long));
            info.AddValue("_datFileContents", _datFileContents, typeof(string));
            info.AddValue("_statusFileLength", _statusFileLength, typeof(long));
            info.AddValue("_statusFileContents", _statusFileContents, typeof(string));
            info.AddValue("_convergenceFileLength", _convergenceFileLength, typeof(long));
            info.AddValue("_convergenceFileContents", _convergenceFileContents, typeof(string));
            info.AddValue("_endTime", _endTime, typeof(DateTime));
            info.AddValue("_femSolver", _femSolver, typeof(FEMSolverEnum));
            info.AddValue("_onlyCheckModel", _onlyCheckModel, typeof(bool));
            info.AddValue("_numCPUsOverridden", _numCPUsOverridden, typeof(bool));
            info.AddValue("_environmentVariablesOverridden", _environmentVariablesOverridden, typeof(bool));
            info.AddValue("_executableOverridden", _executableOverridden, typeof(bool));
            info.AddValue("_workDirectoryOverridden", _workDirectoryOverridden, typeof(bool));
        }
    }
}
