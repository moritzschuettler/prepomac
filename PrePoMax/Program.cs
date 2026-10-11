// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CommandLine;
using CommandLine.Text;
using PrePoMax.Python;
using PrePoMax.Python.Journal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrePoMax
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        //
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
        //
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        //
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
        //
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);
        private delegate bool ConsoleCtrlHandler(int controlType);
        private const int CTRL_C_EVENT = 0;
        private const int CTRL_BREAK_EVENT = 1;
        // PrePoMax.com names the same event - it cannot reference this assembly
        public const string CancelEventNamePrefix = "PrePoMax.Cancel.";
        //
        private static FrmMain _mainForm;
        private static CommandLineOptions _cmdOptions;
        private static System.Threading.Timer _timeoutTimer;
        private static ConsoleCtrlHandler _consoleCtrlHandler;
        private static EventWaitHandle _cancelEvent;
        private static int _isFinishing;
        private static int _isCancelled;
        //
        //[DllImport("kernel32.dll")]
        //static extern bool FreeConsole();
        //[DllImport("user32.dll")]
        //private static extern bool SetProcessDPIAware();
        //
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // DPI
            //if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            AttachToParentConsole();
            Console.WriteLine("");
            //
            SetCultureAndLanguage();
            // Parse
            if (args != null && args.Length == 1 && File.Exists(args[0])) args = new string[] { "-f", args[0] };
            //
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-r0" || args[i] == "-r1" || args[i] == "-r2" || args[i] == "-r3") args[i] = "-" + args[i];
            }
            var parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            //
            if (parserResult.Value != null) Run(parserResult.Value, string.Join(" ", args));
            else Finish(GetParserExitCode(parserResult), null);
            //
            return;
        }
        private static void AttachToParentConsole()
        {
            // Windows gives no console handles to a window subsystem process, so the console of the launcher
            // deployed as PrePoMax.com is joined here. A redirected handle is inherited and must survive the
            // attach, which replaces the standard handles. Nothing may use System.Console before this runs -
            // the writers are created on first use and would bind to the handles that are replaced here.
            IntPtr standardOutput = GetStdHandle(STD_OUTPUT_HANDLE);
            IntPtr standardError = GetStdHandle(STD_ERROR_HANDLE);
            //
            if (standardOutput != IntPtr.Zero && standardError != IntPtr.Zero) return;
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) return;
            //
            if (standardOutput != IntPtr.Zero) SetStdHandle(STD_OUTPUT_HANDLE, standardOutput);
            if (standardError != IntPtr.Zero) SetStdHandle(STD_ERROR_HANDLE, standardError);
        }
        private static void SetCultureAndLanguage()
        {
            System.Globalization.CultureInfo ci =
                (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
            ci.NumberFormat.NumberGroupSeparator = "";
            //
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;           // This thread
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci;   // All feature threads
            //
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
            // Set MessageBoxButtons to English defaults
            MessageBoxManager.OK = "OK";
            MessageBoxManager.Cancel = "Cancel";
            MessageBoxManager.Abort = "Abort";
            MessageBoxManager.Retry = "Retry";
            MessageBoxManager.Ignore = "Ignore";
            MessageBoxManager.Yes = "Yes";
            MessageBoxManager.No = "No";
            MessageBoxManager.Register();
        }
        private static void Run(CommandLineOptions cmdOptions, string arguments)
        {
            try
            {
                _cmdOptions = cmdOptions;
                // Show values
                string values = CommandLineOptions.GetValuesAsString(cmdOptions);
                if (values != null) Console.WriteLine(values);
                // Check for errors - the report location is not known yet, so no report is written
                string cmdError = CommandLineOptions.CheckForErrors(cmdOptions);
                if (cmdError != null)
                {
                    Console.WriteLine("----------Error---------------");
                    Console.WriteLine(cmdError);
                    Console.WriteLine("----------Finished------------");
                    Console.WriteLine("Process finished with errors.");
                    Console.WriteLine("");
                    //
                    ExitProcess(ExitCodeEnum.OptionError);
                }
                // Unattended run contract
                BatchRunReport.Initialize(cmdOptions, arguments);
                SettingsContainer.OverrideFileName = cmdOptions.SettingsFileName;
                SettingsContainer.IsSaveDisabled = cmdOptions.GetIsSettingsSaveDisabled();
                StartTimeoutTimer(cmdOptions);
                StartCancelListener();
                //
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.AddMessageFilter(new UserControls.NoBeepMessageFilter());
                //
                using (FrmMain mainForm = new FrmMain(cmdOptions))
                {
                    _mainForm = mainForm;
                    //
                    if (cmdOptions.ShowGui == "No")   // must be here
                    {
                        // Form.ShowParams returns SW_SHOWMINIMIZED for a minimized window before it considers
                        // ShowWithoutActivation, and that show command takes the foreground; a window placed off
                        // screen is shown with SW_SHOWNOACTIVATE and never steals focus from the user
                        mainForm.StartPosition = FormStartPosition.Manual;
                        mainForm.Location = new System.Drawing.Point(-32000, -32000);
                        mainForm.ShowInTaskbar = false;
                    }
                    Application.ThreadException += Application_ThreadException;
                    Application.Run(mainForm);
                    // A run that kept its window open after its script or regeneration failed still ends with
                    // the code of that failure
                    Exception batchPhaseError = mainForm.BatchPhaseError;
                    if (batchPhaseError != null) FinishedWithException(batchPhaseError);
                    //
                    Console.WriteLine("----------Finished------------");
                    Console.WriteLine("Process finished successfully.");
                    Console.WriteLine("");
                    //
                    Finish(ExitCodeEnum.Ok, null);
                }
            }
            catch (Exception ex)
            {
                FinishedWithException(ex);
            }
        }
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            FinishedWithException(e.Exception);
        }
        private static void FinishedWithException(Exception ex)
        {
            Console.WriteLine("----------Error---------------");
            Console.WriteLine(ex.Message);
            Console.WriteLine("----------Finished------------");
            Console.WriteLine("Process finished with errors.");
            Console.WriteLine("");
            //
            Finish(GetExitCode(ex), ex);
        }
        // The run ends once, whichever of the end of the application, the time limit and a cancel gets here first;
        // a later one waits for the process to be terminated by the first
        private static void Finish(ExitCodeEnum exitCode, Exception exception)
        {
            if (!TryClaimFinish()) Thread.Sleep(System.Threading.Timeout.Infinite);
            //
            FinishClaimed(exitCode, exception);
        }
        private static bool TryClaimFinish()
        {
            return Interlocked.Exchange(ref _isFinishing, 1) == 0;
        }
        private static void FinishClaimed(ExitCodeEnum exitCode, Exception exception)
        {
            StopTimeoutTimer();
            // The journal is written first, so the report can say whether it was
            Controller controller = GetController();
            string journalError;
            bool isJournalWritten = WriteSessionJournal(controller, out journalError);
            //
            BatchRunReport report = BatchRunReport.Current;
            if (report != null)
            {
                report.SetJournalResult(isJournalWritten, journalError);
                report.Write(exitCode, exception, controller);
            }
            //
            ExitProcess(exitCode);
        }
        private static bool WriteSessionJournal(Controller controller, out string error)
        {
            error = null;
            if (_cmdOptions == null || _cmdOptions.JournalFileName == null) return false;
            //
            if (controller == null)
            {
                error = "The application ended before it had a model to journal.";
                Console.WriteLine("The session journal was not written: " + error);
                return false;
            }
            //
            try
            {
                string source = _cmdOptions.FileName != null ? _cmdOptions.FileName : _cmdOptions.RegenerationFileName;
                PythonJournalReport journalReport =
                    PythonJournalExporter.Export(controller.GetActiveCommands(), _cmdOptions.JournalFileName, source,
                                                 controller.Settings.General.NumOfSplitFaces,
                                                 controller.Settings.GetWorkDirectory());
                Console.WriteLine(journalReport.GetSummary());
                foreach (string refusal in journalReport.Refusals) Console.WriteLine("   Not emitted: " + refusal);
                return true;
            }
            catch (Exception ex)
            {
                // The journal is a record of the run, so failing to write it must not change its outcome
                error = ex.Message;
                Console.WriteLine("The session journal was not written: " + ex.Message);
                return false;
            }
        }
        private static void ExitProcess(ExitCodeEnum exitCode)
        {
            // A process remains running after the application exits, so it is terminated with an explicit exit code
            Environment.ExitCode = (int)exitCode;
            TerminateProcess(GetCurrentProcess(), (uint)exitCode);
        }
        private static Controller GetController()
        {
            if (_mainForm == null) return null;
            //
            try
            {
                return _mainForm.Controller;
            }
            catch
            {
                // The report must not fail because the form was already disposed
                return null;
            }
        }
        // A refused prompt is the code only when the refusal is what ended the run. A script may catch a refusal
        // and fail later for a reason of its own, which is a script error like any other
        private static ExitCodeEnum GetExitCode(Exception ex)
        {
            Exception current = ex;
            while (current != null)
            {
                if (current is PromptRefusedException) return ExitCodeEnum.PromptRefused;
                current = current.InnerException;
            }
            //
            current = ex;
            while (current != null)
            {
                if (current is PythonScriptException)
                {
                    if (IsRefusalTheFinalPythonError(current.Message)) return ExitCodeEnum.PromptRefused;
                    return ExitCodeEnum.ScriptError;
                }
                if (current is Rpc.RpcStartException) return ExitCodeEnum.OptionError;
                current = current.InnerException;
            }
            // A regeneration collects the errors of its commands as text and goes on, so the first of them is
            // what made it fail
            Controller controller = GetController();
            List<string> errors = controller == null ? null : controller.GetCommandCollectionErrors();
            if (errors != null && errors.Count > 0 && errors[0] != null &&
                errors[0].Contains(PromptRefusedException.RefusalText))
                return ExitCodeEnum.PromptRefused;
            //
            return ExitCodeEnum.ModelError;
        }
        // A prompt refused inside a Python script does not survive as a typed exception, because the Python runtime
        // carries the traceback as text. The exception the script ended with is the last one the text shows: the
        // lines after the frames of its last traceback, which a chained exception prints after the ones before it
        private static bool IsRefusalTheFinalPythonError(string error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            //
            const string tracebackHeader = "Traceback (most recent call last):";
            int start = error.LastIndexOf(tracebackHeader, StringComparison.Ordinal);
            if (start < 0) return error.Contains(PromptRefusedException.RefusalText);
            //
            string[] lines = error.Substring(start + tracebackHeader.Length).Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0 || char.IsWhiteSpace(lines[i][0])) continue;
                // The first line that is not a frame opens the exception
                return string.Join("\n", lines, i, lines.Length - i).Contains(PromptRefusedException.RefusalText);
            }
            return false;
        }
        private static ExitCodeEnum GetParserExitCode(ParserResult<CommandLineOptions> parserResult)
        {
            NotParsed<CommandLineOptions> notParsed = parserResult as NotParsed<CommandLineOptions>;
            if (notParsed != null)
            {
                foreach (Error error in notParsed.Errors)
                {
                    // Printing the help or the version text is not a failure
                    if (error.Tag == ErrorType.HelpRequestedError || error.Tag == ErrorType.HelpVerbRequestedError ||
                        error.Tag == ErrorType.VersionRequestedError)
                        return ExitCodeEnum.Ok;
                }
            }
            //
            return ExitCodeEnum.OptionError;
        }
        private static void StartTimeoutTimer(CommandLineOptions cmdOptions)
        {
            if (cmdOptions.Timeout <= 0) return;
            //
            _timeoutTimer = new System.Threading.Timer(OnTimeoutElapsed, null, (long)cmdOptions.Timeout * 1000,
                                                       System.Threading.Timeout.Infinite);
        }
        // The time limit covers the batch phase of a run; a run that goes on as an ordinary session stops it
        public static void StopTimeoutTimer()
        {
            System.Threading.Timer timer = _timeoutTimer;
            _timeoutTimer = null;
            if (timer != null) timer.Dispose();
        }
        // Ctrl+C reaches this process when it is attached to the console of PrePoMax.com, and PrePoMax.com also
        // signals the event named after this process, which reaches it in any case. Either ends the run the way the
        // time limit does, so the journal and the report are written; the launcher kills the process after a grace
        // period, or at once on a second Ctrl+C
        private static void StartCancelListener()
        {
            try
            {
                _cancelEvent = new EventWaitHandle(false, EventResetMode.ManualReset,
                                                   CancelEventNamePrefix + Process.GetCurrentProcess().Id);
                ThreadPool.RegisterWaitForSingleObject(_cancelEvent, OnCancelSignaled, null,
                                                       System.Threading.Timeout.Infinite, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("A cancel from PrePoMax.com will end the run without its report: " + ex.Message);
            }
            _consoleCtrlHandler = OnConsoleCtrl;
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);
        }
        private static bool OnConsoleCtrl(int controlType)
        {
            if (controlType != CTRL_C_EVENT && controlType != CTRL_BREAK_EVENT) return false;
            //
            ThreadPool.QueueUserWorkItem(delegate (object state) { Cancel(); });
            return true;
        }
        private static void OnCancelSignaled(object state, bool timedOut)
        {
            Cancel();
        }
        private static void Cancel()
        {
            // Ctrl+C reaches an attached process twice, through the console and through the event
            if (Interlocked.Exchange(ref _isCancelled, 1) == 1) return;
            // The end is claimed before the analyses are killed: a script that ends because its analysis was killed
            // would otherwise finish first, with the code of its own failure. A run already finishing keeps its code
            if (!TryClaimFinish()) return;
            //
            string message = "The run was cancelled with Ctrl+C.";
            Console.WriteLine("----------Cancelled-----------");
            Console.WriteLine(message);
            Console.WriteLine("----------Finished------------");
            Console.WriteLine("Process finished with errors.");
            Console.WriteLine("");
            //
            try
            {
                Controller controller = GetController();
                if (controller != null) controller.KillAllJobs();
            }
            catch
            {
                // Best effort - the process is terminated in any case
            }
            //
            FinishClaimed(ExitCodeEnum.Cancelled, new CaeException(message));
        }
        private static void OnTimeoutElapsed(object state)
        {
            if (!TryClaimFinish()) return;
            //
            string message = "The run exceeded the time limit of " + _cmdOptions.Timeout + " s.";
            Console.WriteLine("----------Timeout-------------");
            Console.WriteLine(message);
            Console.WriteLine("----------Finished------------");
            Console.WriteLine("Process finished with errors.");
            Console.WriteLine("");
            // Kill the running analyses so that no solver process is left behind
            try
            {
                Controller controller = GetController();
                if (controller != null) controller.KillAllJobs();
            }
            catch
            {
                // The watchdog is best effort - the process is terminated in any case
            }
            //
            FinishClaimed(ExitCodeEnum.TimedOut, new CaeException(message));
        }

        //private static bool IsWindowsApplication()
        //{
        //    return GetConsoleWindow() == IntPtr.Zero;
        //}
    }
}

