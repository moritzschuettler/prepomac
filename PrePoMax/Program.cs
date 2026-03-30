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
            if (parserResult.Value != null) Run(parserResult.Value);
            //
            Process.GetCurrentProcess().Kill(); // a process remains running afer application exits
            return;
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
        private static void Run(CommandLineOptions cmdOptions)
        {
            try
            {
                // Show values
                string values = CommandLineOptions.GetValuesAsString(cmdOptions);
                if (values != null) Console.WriteLine(values);
                // Check for errors
                string cmdError = CommandLineOptions.CheckForErrors(cmdOptions);
                if (cmdError != null) throw new CaeException(cmdError);
                //
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //
                using (FrmMain mainForm = new FrmMain(cmdOptions))
                {
                    if (cmdOptions.ShowGui == "No")   // must be here
                    {
                        mainForm.WindowState = FormWindowState.Minimized;
                        mainForm.ShowInTaskbar = false;
                    }
                    Application.ThreadException += Application_ThreadException;
                    Application.Run(mainForm);
                    //
                    Console.WriteLine("----------Finished------------");
                    Console.WriteLine("Process finished successfully.");
                    Console.WriteLine("");
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
            Process.GetCurrentProcess().Kill(); // a process remains running afer application exits
        }

        //private static bool IsWindowsApplication()
        //{
        //    return GetConsoleWindow() == IntPtr.Zero;
        //}
    }
}

