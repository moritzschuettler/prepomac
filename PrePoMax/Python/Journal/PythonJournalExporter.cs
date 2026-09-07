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
using System.IO;
using System.Text;
using PrePoMax.Commands;

namespace PrePoMax.Python.Journal
{
    // Turns a command history into a Python script. The commands say what they are; this drives them,
    // writes the header, and collects what could not be emitted.
    public static class PythonJournalExporter
    {
        // Methods                                                                                                                  
        public static PythonJournalReport Export(List<Command> commands, string fileName, string sourceDescription)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            //
            PythonJournalReport report;
            string text = GetText(commands, fileName, sourceDescription, out report);
            //
            File.WriteAllText(fileName, text, new UTF8Encoding(false));
            //
            return report;
        }
        public static string GetText(List<Command> commands, string fileName, string sourceDescription,
                                     out PythonJournalReport report)
        {
            PythonJournalWriter writer = new PythonJournalWriter();
            int commandCount = commands == null ? 0 : commands.Count;
            //
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    Command command = commands[i];
                    if (command == null) continue;
                    //
                    writer.CommandIndex = i;
                    try
                    {
                        command.ToPython(writer);
                    }
                    catch (Exception ex)
                    {
                        writer.Unsupported(command, ex.Message);
                    }
                }
            }
            //
            report = new PythonJournalReport(fileName, commandCount, writer.EmittedCommandCount, writer.Refusals);
            //
            return GetHeader(fileName, sourceDescription, report) + writer.GetText();
        }
        private static string GetHeader(string fileName, string sourceDescription, PythonJournalReport report)
        {
            string scriptName = fileName == null ? "journal.py" : Path.GetFileName(fileName);
            StringBuilder header = new StringBuilder();
            //
            header.AppendLine("# PrePoMax session journal");
            header.AppendLine("# The PrePoMax object is available as 'pmx'.");
            header.AppendLine("#");
            header.AppendLine("# Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " by " + Globals.ProgramName);
            if (sourceDescription != null) header.AppendLine("# Source: " + sourceDescription);
            header.AppendLine("# " + report.CommandCount + " commands, " + report.RefusedCommandCount + " not emitted");
            header.AppendLine("#");
            header.AppendLine("#   PrePoMax.com --script " + scriptName + " -g No -x Yes -w <work directory>");
            header.AppendLine();
            //
            return header.ToString();
        }
    }
}
