using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaeMesh.Meshing;

namespace GmshCaller
{
    internal class Program
    {
        static int Main(string[] args)
        {
            string error = null;
            if (args.Length == 2)
            {
                string gmshDataFileName = args[0];
                if (File.Exists(gmshDataFileName))
                {
                    GmshData gmshData = Tools.LoadDumpFromFile<GmshData>(gmshDataFileName);
                    //
                    GmshAPI gmshAPI = new GmshAPI(gmshData, Console.WriteLine);
                    //
                    GmshCommandEnum command;
                    if (Enum.TryParse(args[1], out command))
                    {
                        if (command == GmshCommandEnum.Mesh) error = gmshAPI.CreateMesh();
                        else if (command == GmshCommandEnum.Defeature) error = gmshAPI.Defeature();
                        else error = "The Gmsh command " + args[1] + " is not supported.";
                    }
                    else error = "The Gmsh command " + args[1] + " is not supported.";
                }
            }
            else error = "A Gmsh data file name and a Gmsh command parameters must be specified as arguments.";
            //Console.WriteLine("Press any key to stop...");
            //Console.ReadKey();
            //
            Console.Error.WriteLine(error);
            //
            if (error != null) return 1;
            else return 0;
        }
    }
}
