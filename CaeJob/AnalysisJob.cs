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
        Abaqus
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
                    if (_femSolver == FEMSolverEnum.Calculix) _argument = Name;
                    else if (_femSolver == FEMSolverEnum.Abaqus) _argument = "job=" + Name;
                    else throw new NotSupportedException();
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
                else throw new NotSupportedException();
            }
        }
        public bool IsUpToDate
        {
            get
            {
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
                    if (_femSolver == FEMSolverEnum.Calculix) _argument = Name;
                    else if (_femSolver == FEMSolverEnum.Abaqus) _argument = "job=" + Name;
                    else throw new NotSupportedException();
                }
            }
        }
        public bool OnlyCheckModel { get { return _onlyCheckModel; } set { _onlyCheckModel = value; } }
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
            _onlyCheckModel = false;
        }
        public AnalysisJob(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Compatibility v 2.3.5
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
                    default:
                        break;
                }
            }
        }

        // Event handlers                                                                                                           
        void Timer_Tick(object sender, EventArgs e)
        {
            //OutputData();
        }


        // Methods                                                                                                                  
        public void Submit(int numOfRunSteps, int numOfRunIncrements, bool useBackgroundWorker = true)
        {
            if (numOfRunSteps < 1 || numOfRunIncrements < 1) throw new NotSupportedException();
            _useBackgroundWorker = useBackgroundWorker;
            // Reset job
            _tag = null;
            _jobStatus = JobStatus.None;
            //
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
            }
            //
            AppendDataToOutput(DateTime.Now + Environment.NewLine);
            AppendDataToOutput("########   Starting run step number: " + _currentRunStep +
                               "   Increment number: "  + _currentRunIncrement + "   ########" +  Environment.NewLine);
            AppendDataToOutput("Running command: " + _executable + " " + _argument);
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
            if (Tools.IsWindows10orNewer() && !_compatibilityMode) Run_Win10();
            else Run_OldWin();
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
            int ms = 1000 * 3600 * 24 * 7 * 3; // 3 weeks
            if (_exe.WaitForInputIdle(ms))
            {
                // Process completed. Check process.ExitCode here.
                // after Kill() _jobStatus is Killed
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
        private void Run_OldWin()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _executable;
            psi.Arguments = _argument;
            psi.WorkingDirectory = _workDirectory;
            psi.UseShellExecute = false;
            //
            //SetEnvironmentVariables(psi);
            //
            _exe = new Process();
            _exe.StartInfo = psi;
            _exe.Start();
            //
            int ms = 1000 * 3600 * 24 * 7 * 3; // 3 weeks
            if (_exe.WaitForExit(ms))
            {
                // Process completed. Check process.ExitCode here.

                // after Kill() _jobStatus is Killed
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
        private void Run_Win10()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.FileName = _executable;
            //
            if (_femSolver == FEMSolverEnum.Calculix) psi.Arguments = _argument;
            else if (_femSolver == FEMSolverEnum.Abaqus)
            {
                psi.Arguments = "interactive ask_delete=OFF " + _argument;
                if (_onlyCheckModel) psi.Arguments = "datacheck " + psi.Arguments;
            }
            else throw new NotSupportedException();
            //
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
                //
                _exe.BeginOutputReadLine();
                _exe.BeginErrorReadLine();
                int ms = 1000 * 3600 * 24 * 7 * 3; // 3 weeks
                if (_exe.WaitForExit(ms) && outputWaitHandle.WaitOne(ms) && errorWaitHandle.WaitOne(ms))
                {
                    // Process completed. Check process.ExitCode here.
                    // after Kill() _jobStatus is Killed
                    if (_jobStatus != JobStatus.Killed) _jobStatus = JobStatus.OK;
                }
                else
                {
                    // Timed out.
                    Kill("Time out.");
                    //Debug.WriteLine(DateTime.Now + "   Timeout proces: " + Name + " in: " + _workDirectory);
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
        public static void KillAllProcessesSpawnedBy(UInt32 parentProcessId)
        {
            // NOTE: Process Ids are reused!
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
        private void SendDataToOutput()
        {
            if (_myLock == null) _myLock = new object();
            lock (_myLock)
            {
                string outputData = _sbOutput.ToString();
                //
                if (_outputFileName != null) File.AppendAllText(_outputFileName, outputData);
                //
                GetDatFileContents();
                GetStatusFileContents();
                GetConvergenceFileContents();
                //
                DataOutputEvent?.Invoke(this, outputData);
                //
                if (_sbAllOutput.Length > 2_000_000_000) Kill("The string builder run out of space.");
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
                                                "Warning: .dat file is to log to be displayed in the Monitor form.";
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
        }
        //
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
            if (_environmentVariables != null)
            {
                foreach (var environmentVariable in _environmentVariables)
                {
                    SetEnvironmentVariable(psi, environmentVariable);
                }
            }
        }
        private void SetNumberOfProcessors(ProcessStartInfo psi)
        {
            EnvironmentVariable numberOfProcessors = new EnvironmentVariable("OMP_NUM_THREADS", _numCPUs.ToString());
            SetEnvironmentVariable(psi, numberOfProcessors);
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
        }
    }
}
