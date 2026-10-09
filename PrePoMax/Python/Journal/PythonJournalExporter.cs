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
        public static PythonJournalReport Export(List<Command> commands, string fileName, string sourceDescription,
                                                 int numOfLegacySplitFaces = 1)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            //
            PythonJournalReport report;
            string text = GetText(commands, fileName, sourceDescription, out report, numOfLegacySplitFaces);
            //
            File.WriteAllText(fileName, text, new UTF8Encoding(false));
            //
            return report;
        }
        public static string GetText(List<Command> commands, string fileName, string sourceDescription,
                                     out PythonJournalReport report, int numOfLegacySplitFaces = 1)
        {
            int[] commandFirstLineNumbers;
            return GetText(commands, fileName, sourceDescription, out report, out commandFirstLineNumbers,
                           numOfLegacySplitFaces);
        }
        // The line numbers say where each command's own lines start, so that a caller showing the script next to the
        // command history can point at the lines a selected command produced
        public static string GetText(List<Command> commands, string fileName, string sourceDescription,
                                     out PythonJournalReport report, out int[] commandFirstLineNumbers,
                                     int numOfLegacySplitFaces = 1)
        {
            PythonJournalWriter writer = new PythonJournalWriter();
            writer.NumOfLegacySplitFaces = numOfLegacySplitFaces;
            int commandCount = commands == null ? 0 : commands.Count;
            //
            if (commands != null)
            {
                // The Netgen items of an older model close its history, but the application meshes that model
                // the way they say from the start, so the journal adds them before the first meshing
                int netgenMeshIndex = -1;
                int firstMeshingIndex = -1;
                for (int i = 0; i < commands.Count; i++)
                {
                    if (commands[i] is CAddNetgenMeshToUncoveredParts) netgenMeshIndex = i;
                    else if (firstMeshingIndex < 0 && commands[i] is CCreateMesh) firstMeshingIndex = i;
                }
                bool moveNetgenMesh = firstMeshingIndex >= 0 && netgenMeshIndex > firstMeshingIndex;
                //
                for (int i = 0; i < commands.Count; i++)
                {
                    Command command = commands[i];
                    writer.BeginCommand(i);
                    if (command == null) continue;
                    //
                    if (moveNetgenMesh && i == firstMeshingIndex) EmitCommand(writer, commands[netgenMeshIndex]);
                    if (moveNetgenMesh && i == netgenMeshIndex)
                    {
                        writer.Comment(command.Name + ": its mesh setup items are added before the first meshing");
                        continue;
                    }
                    EmitCommand(writer, command);
                }
            }
            //
            report = new PythonJournalReport(fileName, commandCount, writer.EmittedCommandCount, writer.Refusals);
            //
            string header = GetHeader(fileName, sourceDescription, report);
            int headerLineCount = GetLineCount(header);
            //
            commandFirstLineNumbers = new int[writer.CommandFirstLineIndices.Count];
            for (int i = 0; i < commandFirstLineNumbers.Length; i++)
                commandFirstLineNumbers[i] = headerLineCount + writer.CommandFirstLineIndices[i] + 1;
            //
            return header + writer.GetText();
        }
        private static void EmitCommand(PythonJournalWriter writer, Command command)
        {
            try
            {
                command.ToPython(writer);
            }
            catch (Exception ex)
            {
                writer.Unsupported(command, ex.Message);
            }
        }
        private static int GetLineCount(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }
            return count;
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
