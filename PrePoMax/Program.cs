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
        private static FrmMain _mainForm;
        private static CommandLineOptions _cmdOptions;
        private static System.Threading.Timer _timeoutTimer;
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
        private static void Finish(ExitCodeEnum exitCode, Exception exception)
        {
            StopTimeoutTimer();
            //
            BatchRunReport report = BatchRunReport.Current;
            if (report != null) report.Write(exitCode, exception, GetController());
            //
            WriteSessionJournal();
            //
            ExitProcess(exitCode);
        }
        private static void WriteSessionJournal()
        {
            if (_cmdOptions == null || _cmdOptions.JournalFileName == null) return;
            //
            Controller controller = GetController();
            if (controller == null) return;
            //
            try
            {
                string source = _cmdOptions.FileName != null ? _cmdOptions.FileName : _cmdOptions.RegenerationFileName;
                PythonJournalReport journalReport = PythonJournalExporter.Export(controller.GetCommands(),
                                                                                 _cmdOptions.JournalFileName, source);
                Console.WriteLine(journalReport.GetSummary());
                foreach (string refusal in journalReport.Refusals) Console.WriteLine("   Not emitted: " + refusal);
            }
            catch (Exception ex)
            {
                // The journal is a record of the run, so failing to write it must not change its outcome
                Console.WriteLine("The session journal was not written: " + ex.Message);
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
        private static ExitCodeEnum GetExitCode(Exception ex)
        {
            // A refused prompt is the more specific cause, so the whole chain is searched for it before
            // the run is written off as a script error
            Exception current = ex;
            while (current != null)
            {
                if (current is PromptRefusedException) return ExitCodeEnum.PromptRefused;
                current = current.InnerException;
            }
            // A prompt refused inside a Python script does not survive as a typed exception, because the
            // Python runtime carries the traceback as text, so the run report is the record of it
            BatchRunReport report = BatchRunReport.Current;
            if (report != null && report.HasRefusedPrompt()) return ExitCodeEnum.PromptRefused;
            //
            current = ex;
            while (current != null)
            {
                if (current is PythonScriptException) return ExitCodeEnum.ScriptError;
                current = current.InnerException;
            }
            //
            return ExitCodeEnum.ModelError;
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
        private static void StopTimeoutTimer()
        {
            System.Threading.Timer timer = _timeoutTimer;
            _timeoutTimer = null;
            if (timer != null) timer.Dispose();
        }
        private static void OnTimeoutElapsed(object state)
        {
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
            Finish(ExitCodeEnum.TimedOut, new CaeException(message));
        }

        //private static bool IsWindowsApplication()
        //{
        //    return GetConsoleWindow() == IntPtr.Zero;
        //}
    }
}

