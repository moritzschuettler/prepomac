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
                                                 int numOfLegacySplitFaces = 1, string workDirectory = null)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            //
            PythonJournalReport report;
            string text = GetText(commands, fileName, sourceDescription, out report, numOfLegacySplitFaces,
                                  workDirectory);
            //
            File.WriteAllText(fileName, text, new UTF8Encoding(false));
            //
            return report;
        }
        public static string GetText(List<Command> commands, string fileName, string sourceDescription,
                                     out PythonJournalReport report, int numOfLegacySplitFaces = 1,
                                     string workDirectory = null)
        {
            int[] commandFirstLineNumbers;
            return GetText(commands, fileName, sourceDescription, out report, out commandFirstLineNumbers,
                           numOfLegacySplitFaces, workDirectory);
        }
        // The line numbers say where each command's own lines start, so that a caller showing the script next to the
        // command history can point at the lines a selected command produced
        public static string GetText(List<Command> commands, string fileName, string sourceDescription,
                                     out PythonJournalReport report, out int[] commandFirstLineNumbers,
                                     int numOfLegacySplitFaces = 1, string workDirectory = null)
        {
            PythonJournalWriter writer = new PythonJournalWriter();
            writer.NumOfLegacySplitFaces = numOfLegacySplitFaces;
            writer.WorkDirectory = workDirectory;
            int commandCount = commands == null ? 0 : commands.Count;
            //
            if (commands != null)
            {
                // The Netgen items of an older model close its history, but the application meshes that model
                // the way they say from the start, so the journal adds them before the first meshing. The items
                // select the geometry parts the history ended with, so they stay where they are when a command
                // between the two changes those parts.
                int netgenMeshIndex = -1;
                int firstMeshingIndex = -1;
                for (int i = 0; i < commands.Count; i++)
                {
                    if (commands[i] is CAddNetgenMeshToUncoveredParts) netgenMeshIndex = i;
                    else if (firstMeshingIndex < 0 && commands[i] is CCreateMesh) firstMeshingIndex = i;
                }
                bool moveNetgenMesh = firstMeshingIndex >= 0 && netgenMeshIndex > firstMeshingIndex;
                for (int i = firstMeshingIndex + 1; moveNetgenMesh && i < netgenMeshIndex; i++)
                {
                    if (ChangesGeometryParts(commands[i])) moveNetgenMesh = false;
                }
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
            string header = GetHeader(fileName, sourceDescription, report, writer.WritesToWorkDirectory,
                                      writer.SessionStartFileName);
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
        // A command that adds, removes, renames or rebuilds geometry parts, or one that can do anything, such as a
        // recorded script; hiding, showing and colouring parts leave them as they are
        private static bool ChangesGeometryParts(Command command)
        {
            if (command == null) return false;
            if (command is CRenamePre rename)
                return rename.ItemType != null && typeof(CaeMesh.BasePart).IsAssignableFrom(rename.ItemType);
            //
            return command is CImportFile || command is COpenFile || command is CClear || command is CRunPythonScript ||
                   command is CCreateAndImportCompoundPart || command is CRegenerateCompoundParts ||
                   command is CMirrorGeometryParts || command is CRemoveGeometryParts ||
                   command is CRotateGeometryParts || command is CScaleGeometryParts ||
                   command is CTranslateGeometryParts || command is CReplaceGeometryPartProperties ||
                   command is CFindEdgesByAngleForGeometryParts || command is CFuseBodies ||
                   command is CConvertShellToSolid || command is CConvertSolidToShell || command is CDefeature ||
                   command is CDeleteCadPartFaces || command is CDeleteStlPartFaces ||
                   command is CDeleteStlPartFacesCommand || command is CFlipFaceOrientations ||
                   command is FlipStlPartSurfacesNormal || command is CFuseFaces || command is COffsetFace ||
                   command is CRepairGeometry || command is CSplitGeometry || command is CSplitPeriodicFaces ||
                   command is CSwapPartGeometries || command is CGroupSplitCells || command is CRenameSplitSubPart ||
                   command is CSplitAFaceUsingTwoPoints || command is CExtractSurface;
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
        private static string GetHeader(string fileName, string sourceDescription, PythonJournalReport report,
                                        bool writesToWorkDirectory, string sessionStartFileName)
        {
            string scriptName = fileName == null ? "journal.py" : Path.GetFileName(fileName);
            StringBuilder header = new StringBuilder();
            //
            header.AppendLine("# PrePoMax session journal");
            header.AppendLine("# The PrePoMax object is available as 'pmx'.");
            header.AppendLine("#");
            header.AppendLine("# Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " by " + Globals.ProgramName);
            if (sourceDescription != null) header.AppendLine("# Opened from: " + sourceDescription);
            header.AppendLine("# " + report.CommandCount + " commands, " + report.RefusedCommandCount + " not emitted");
            header.AppendLine("#");
            if (sessionStartFileName == null)
            {
                header.AppendLine("#   PrePoMax.com --script " + scriptName + " -g No -x Yes -w <work directory>");
            }
            else
            {
                // A history that begins by opening a model file goes on from that model, which it cannot rebuild
                header.AppendLine("# The session began by opening a model file, which this journal cannot rebuild,");
                header.AppendLine("# so replay it with that file opened by -f:");
                header.AppendLine("#");
                header.AppendLine("#   PrePoMax.com --script " + scriptName + " -f \"" + sessionStartFileName +
                                  "\" -g No -x Yes -w <work directory>");
            }
            if (writesToWorkDirectory)
            {
                header.AppendLine("#");
                header.AppendLine("# The files the session saved or exported are written to the work directory of the");
                header.AppendLine("# replay, pmx.work_directory, under their names, and read back from there. Two files");
                header.AppendLine("# of the same name get numbered names, name_2.ext. The replay never overwrites a file");
                header.AppendLine("# it did not write itself: one already in the work directory stops it with");
                header.AppendLine("# FileExistsError, so replay in an empty one. A replay recorded as a script and run");
                header.AppendLine("# again - Regenerate, Undo, Redo - finds the files of its first run there and");
                header.AppendLine("# writes over them, which pmx.is_rerun tells it.");
                header.AppendLine();
                header.AppendLine("import os");
                header.AppendLine();
                header.AppendLine("_journal_outputs = set()");
                header.AppendLine();
                header.AppendLine();
                header.AppendLine("def _output(name):");
                header.AppendLine("    path = os.path.join(pmx.work_directory, name)");
                header.AppendLine("    key = os.path.normcase(os.path.abspath(path))");
                header.AppendLine("    if not pmx.is_rerun and key not in _journal_outputs and os.path.exists(path):");
                header.AppendLine("        raise FileExistsError(path + ' exists and was not written by this ' +");
                header.AppendLine("                              'replay; replay in an empty work directory')");
                header.AppendLine("    _journal_outputs.add(key)");
                header.AppendLine("    return path");
                header.AppendLine();
            }
            header.AppendLine();
            //
            return header.ToString();
        }
    }
}
