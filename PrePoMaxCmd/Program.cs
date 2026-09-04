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
        // Mirrors PrePoMax.ExitCodeEnum.LauncherError - the launcher cannot reference the application assembly
        private const int LauncherErrorExitCode = 6;
        //
        private static Process _guiProcess;
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
                    return guiProcess.ExitCode;
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
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // PrePoMax.exe receives the break event only while it is attached to the console
            Process guiProcess = _guiProcess;
            if (guiProcess == null) return;
            //
            e.Cancel = true;
            //
            try
            {
                if (!guiProcess.HasExited) guiProcess.Kill();
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
