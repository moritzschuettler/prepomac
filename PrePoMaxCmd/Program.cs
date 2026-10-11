// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PrePoMaxCmd
{
    // The console subsystem launcher deployed next to PrePoMax.exe as PrePoMax.com. PATHEXT resolves .com
    // before .exe, so a command line run starts this process, which keeps the shell waiting and forwards the
    // exit code that the shell reports as ERRORLEVEL.
    //
    // A redirected standard handle is inherited by PrePoMax.exe and written to directly. A console standard
    // handle is not - Windows gives no console handles to a window subsystem process, whether or not they are
    // passed in STARTF_USESTDHANDLES - so PrePoMax.exe joins the console of this process itself. See
    // AttachToParentConsole in PrePoMax\Program.cs.
    static class Program
    {
        private const string GuiExecutableName = "PrePoMax.exe";
        // Mirror PrePoMax.ExitCodeEnum.LauncherError and Cancelled - the launcher cannot reference the application
        private const int LauncherErrorExitCode = 6;
        private const int CancelledExitCode = 7;
        // The exit code Process.Kill terminates a process with
        private const int KilledExitCode = -1;
        // Mirrors PrePoMax.Program.CancelEventNamePrefix
        private const string CancelEventNamePrefix = "PrePoMax.Cancel.";
        // How long PrePoMax.exe is given to write its journal and report after a cancel before it is killed
        private const int CancelGracePeriodMilliseconds = 30000;
        //
        private static Process _guiProcess;
        private static int _numberOfCancels;
        private static volatile bool _isKilled;
        //
        static int Main()
        {
            string executableFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GuiExecutableName);
            //
            if (!File.Exists(executableFileName))
            {
                Console.Error.WriteLine("The file '" + executableFileName + "' does not exist.");
                return LauncherErrorExitCode;
            }
            //
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(executableFileName, GetForwardedArguments());
                // A redirected standard handle is inherited only when the shell is not used to start the process
                startInfo.UseShellExecute = false;
                //
                using (Process guiProcess = Process.Start(startInfo))
                {
                    _guiProcess = guiProcess;
                    Console.CancelKeyPress += OnCancelKeyPress;
                    //
                    guiProcess.WaitForExit();
                    //
                    // A process that ended on its own before the kill reached it reports its own code. The kill
                    // may end the wait before it is flagged, so a cancel that was asked for counts as well
                    int exitCode = guiProcess.ExitCode;
                    bool isCancelled = _isKilled || Volatile.Read(ref _numberOfCancels) > 0;
                    return isCancelled && exitCode == KilledExitCode ? CancelledExitCode : exitCode;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return LauncherErrorExitCode;
            }
            finally
            {
                _guiProcess = null;
            }
        }
        // The first Ctrl+C asks PrePoMax.exe to end the run, which it does the way its time limit does: it kills the
        // running analyses, writes the journal and the report and exits with code 7. It receives the break event only
        // while it is attached to this console, so it is also asked through an event named after its process. It is
        // killed when it has not exited after the grace period, or at once on a second Ctrl+C
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Process guiProcess = _guiProcess;
            if (guiProcess == null) return;
            //
            e.Cancel = true;
            //
            if (Interlocked.Increment(ref _numberOfCancels) > 1 || !SignalCancel(guiProcess))
            {
                Kill(guiProcess);
                return;
            }
            Console.Error.WriteLine("Cancelling - press Ctrl+C again to stop PrePoMax at once.");
            ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                try
                {
                    if (!guiProcess.WaitForExit(CancelGracePeriodMilliseconds)) Kill(guiProcess);
                }
                catch
                {
                    // The process was already disposed of because it exited
                }
            });
        }
        private static bool SignalCancel(Process guiProcess)
        {
            try
            {
                EventWaitHandle cancelEvent;
                if (!EventWaitHandle.TryOpenExisting(CancelEventNamePrefix + guiProcess.Id, out cancelEvent)) return false;
                using (cancelEvent) cancelEvent.Set();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static void Kill(Process guiProcess)
        {
            try
            {
                if (!guiProcess.HasExited)
                {
                    guiProcess.Kill();
                    _isKilled = true;
                }
            }
            catch
            {
                // The process exited between the check and the kill
            }
        }
        private static string GetForwardedArguments()
        {
            // The arguments are taken from the raw command line so that the original quoting is preserved
            string commandLine = Environment.CommandLine;
            int index;
            //
            if (commandLine.Length > 0 && commandLine[0] == '"')
            {
                index = commandLine.IndexOf('"', 1);
                index = index < 0 ? commandLine.Length : index + 1;
            }
            else
            {
                index = 0;
                while (index < commandLine.Length && !char.IsWhiteSpace(commandLine[index])) index++;
            }
            //
            return commandLine.Substring(index).TrimStart();
        }
    }
}
