using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMaxCmd
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string arguments = "";
            if (args != null && args.Length > 0)
            {
                foreach (string arg in args) arguments += arg + " ";
            }
            //
            var startInfo = new ProcessStartInfo
            {
                FileName = "PrePoMax.exe", // Replace with your console application path
                Arguments = arguments,
                UseShellExecute = false
            };
            //
            var p = Process.Start(startInfo);
            p.WaitForExit();
             
        }
    }
}
