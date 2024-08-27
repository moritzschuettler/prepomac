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
using System.Threading.Tasks;
using System.Windows.Forms;
using static CaeGlobals.Geometry2;
using static System.Windows.Forms.Design.AxImporter;

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
        static extern bool FreeConsole();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //if (IsWindowsApplication()) AttachConsole(ATTACH_PARENT_PROCESS);
            Console.WriteLine("");
            //
            SetCultureAndLanguage();
            //
            var parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            //
            if (parserResult.Value != null) Run(parserResult.Value);
            //
            //if (IsWindowsApplication()) FreeConsole();
            //
            Process.GetCurrentProcess().Kill(); // a process remains running afer application exits
        }
        private static bool IsWindowsApplication()
        {
            return GetConsoleWindow() == IntPtr.Zero;
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
            bool error = false;
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
                    if (cmdOptions.NoGui)   // must be here
                    {
                        mainForm.WindowState = FormWindowState.Minimized;
                        mainForm.ShowInTaskbar = false;
                    }
                    Application.Run(mainForm);
                }
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine("----------Error---------------");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("----------Finished------------");
                if (error) Console.WriteLine("Process finished with errors.");
                else Console.WriteLine("Process finished successfully.");
                Console.WriteLine("");
            }
        }
    }
}

