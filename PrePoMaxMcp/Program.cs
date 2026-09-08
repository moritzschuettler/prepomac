// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PrePoMaxMcp
{
    // The MCP server for PrePoMax. An MCP client starts it and talks to it on stdin and stdout, so
    // nothing but protocol may ever be written to the standard output; everything a human should read
    // goes to the standard error.
    //
    //   PrePoMaxMcp.exe                serve, attaching to the only running session
    //   PrePoMaxMcp.exe --pid <id>     serve, attaching to that session
    //   PrePoMaxMcp.exe --list         print the running sessions and exit
    //   PrePoMaxMcp.exe --help         print this and exit
    public static class Program
    {
        // Static methods                                                                                                           
        public static int Main(string[] arguments)
        {
            int processId = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i];
                if (argument == "--help" || argument == "-h" || argument == "/?")
                {
                    WriteUsage();
                    return 0;
                }
                if (argument == "--list")
                {
                    WriteSessions();
                    return 0;
                }
                if (argument == "--pid")
                {
                    if (i + 1 >= arguments.Length || !int.TryParse(arguments[i + 1], NumberStyles.Integer,
                                                                   CultureInfo.InvariantCulture, out processId))
                    {
                        Console.Error.WriteLine("--pid needs the process id of a running PrePoMax session.");
                        return 1;
                    }
                    i++;
                    continue;
                }
                Console.Error.WriteLine("Unknown argument " + argument + ". Use --help.");
                return 1;
            }
            //
            try
            {
                // The transport is UTF-8 without a byte order mark, which a reader on the other side
                // would otherwise see as part of the first message
                Console.OutputEncoding = new UTF8Encoding(false);
                Console.InputEncoding = new UTF8Encoding(false);
            }
            catch (Exception)
            {
                // A redirected console may refuse the change, and the default is then already right
            }
            //
            McpServer server = new McpServer(Console.In, Console.Out, processId);
            server.Run();
            return 0;
        }
        private static void WriteUsage()
        {
            Console.Error.WriteLine("PrePoMaxMcp - the MCP server for a running PrePoMax session.");
            Console.Error.WriteLine("");
            Console.Error.WriteLine("  PrePoMaxMcp.exe                serve on stdin and stdout");
            Console.Error.WriteLine("  PrePoMaxMcp.exe --pid <id>     serve, attaching to that session");
            Console.Error.WriteLine("  PrePoMaxMcp.exe --list         print the running sessions and exit");
            Console.Error.WriteLine("");
            Console.Error.WriteLine("PrePoMax must be running with its automation server on:");
            Console.Error.WriteLine("Tools - Automation Server, or Tools - Settings - Automation.");
        }
        private static void WriteSessions()
        {
            List<SessionInfo> sessions = SessionLocator.GetSessions();
            if (sessions.Count == 0)
            {
                Console.Error.WriteLine("No PrePoMax session is listening.");
                return;
            }
            foreach (SessionInfo session in sessions) Console.Error.WriteLine(session.ToString());
        }
    }
}
