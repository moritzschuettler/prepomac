// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PmxTool
{
    // Runs from the lib folder of a PrePoMax build, next to the assemblies whose stored types it reads:
    //   PmxTool schema --check <snapshot>    fails when a stored type or field changed its meaning
    //   PmxTool schema --update <snapshot>   rewrites the snapshot, keeping removed entries as retired
    //   PmxTool schema --print               prints the current schema
    //   PmxTool dump <file> [chunk] [--all]  prints a .pmx container, a .pmh journal or a storage payload as text
    public static class Program
    {
        // Methods                                                                                                                  
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            try
            {
                if (args.Length >= 2 && args[0] == "schema" && args[1] == "--print")
                {
                    LoadApplicationAssemblies();
                    foreach (var line in SchemaSnapshot.Create()) Console.WriteLine(line);
                    return 0;
                }
                if (args.Length >= 3 && args[0] == "schema" && (args[1] == "--check" || args[1] == "--update"))
                {
                    LoadApplicationAssemblies();
                    return SchemaSnapshot.Run(args[2], args[1] == "--update");
                }
                if (args.Length >= 2 && args[0] == "dump")
                {
                    LoadApplicationAssemblies();
                    bool all = Array.IndexOf(args, "--all") >= 0;
                    string chunk = args.Length >= 3 && args[2] != "--all" ? args[2] : null;
                    return PayloadDumper.Run(args[1], chunk, all ? int.MaxValue : 20, Console.Out);
                }
                Console.Error.WriteLine("Usage: PmxTool schema --check|--update <snapshot file> | schema --print | " +
                                        "dump <file> [chunk] [--all]");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("PmxTool: " + ex);
                return 2;
            }
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            string folder = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string[] candidates = new string[] { Path.Combine(folder, name + ".dll"), Path.Combine(folder, "..", name + ".exe") };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            }
            return null;
        }
        // The application and everything it references, so that every stored type is registered
        private static void LoadApplicationAssemblies()
        {
            Queue<Assembly> queue = new Queue<Assembly>();
            HashSet<string> visited = new HashSet<string>();
            queue.Enqueue(Assembly.Load("PrePoMax"));
            while (queue.Count > 0)
            {
                foreach (var reference in queue.Dequeue().GetReferencedAssemblies())
                {
                    if (!visited.Add(reference.Name)) continue;
                    try
                    {
                        queue.Enqueue(Assembly.Load(reference));
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
