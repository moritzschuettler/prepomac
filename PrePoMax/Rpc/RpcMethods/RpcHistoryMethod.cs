// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Commands;
using PrePoMax.Python.Journal;
using System;
using System.Collections.Generic;
using System.IO;

namespace PrePoMax.Rpc
{
    // The command history, and on request the Layer 2 journal of it. The journal is the most useful
    // thing an agent can read: it is the session so far written as the Python a user would have
    // written, so an agent learns the right call for what the user did by hand rather than guessing.
    public static class RpcHistoryMethod
    {
        // Variables                                                                                                                
        // The arguments the method takes; any other is refused before anything is done
        public static readonly string[] Parameters = { "limit", "include_journal", "journal_file" };
        private const int DefaultLimit = 200;
        // A journal longer than this is written to a file instead of being returned inline
        private const int InlineJournalLimit = 200000;


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            Controller controller = server.Controller;
            if (controller == null) throw new RpcException(RpcErrorCode.InternalError, "There is no open model.");
            //
            List<Command> commands = controller.GetCommands();
            if (commands == null) commands = new List<Command>();
            int limit = request.GetInt("limit", DefaultLimit);
            if (limit < 0)
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument limit must be 0 or more: 0 lists every command.");
            int first = limit == 0 || limit >= commands.Count ? 0 : commands.Count - limit;
            bool includeJournal = request.GetBool("include_journal", false);
            string journalFile = request.GetString("journal_file", null);
            if (journalFile != null && !includeJournal)
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument journal_file is only used with " +
                                                                   "include_journal set to true.");
            if (journalFile != null) AssertJournalFileName(journalFile);
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("commandCount", commands.Count);
            writer.WriteValue("firstIndex", first);
            writer.BeginArray("commands");
            for (int i = first; i < commands.Count; i++)
            {
                Command command = commands[i];
                writer.BeginObject();
                writer.WriteValue("index", i);
                writer.WriteValue("type", command == null ? "" : command.GetType().Name);
                writer.WriteValue("name", command == null || command.Name == null ? "" : command.Name);
                writer.WriteValue("description", RpcExecMethod.GetDescription(command));
                writer.EndObject();
            }
            writer.EndArray();
            //
            if (includeJournal)
                WriteJournal(writer, journalFile, commands, controller.Settings.General.NumOfSplitFaces,
                             controller.Settings.GetWorkDirectory());
            writer.EndObject();
            return writer.ToString();
        }
        private static void WriteJournal(JsonWriter writer, string requestedFile, List<Command> commands,
                                         int numOfLegacySplitFaces, string workDirectory)
        {
            string fileName = requestedFile == null ? GetTemporaryFileName() : GetRequestedFileName(requestedFile);
            PythonJournalReport report = PythonJournalExporter.Export(commands, fileName, "the automation server",
                                                                      numOfLegacySplitFaces, workDirectory);
            //
            writer.BeginObject("journal");
            writer.WriteValue("emittedCommandCount", report.EmittedCommandCount);
            writer.WriteValue("refusedCommandCount", report.RefusedCommandCount);
            writer.BeginArray("refusals");
            foreach (string refusal in report.Refusals) writer.WriteValue(refusal);
            writer.EndArray();
            //
            string text = requestedFile == null ? ReadAndRemove(fileName) : null;
            if (requestedFile != null || text == null || text.Length > InlineJournalLimit)
            {
                // A journal too long to carry in an answer is left where the caller can read it
                writer.WriteValue("fileName", fileName);
                writer.WriteValue("isInline", false);
            }
            else
            {
                writer.WriteValue("source", text);
                writer.WriteValue("isInline", true);
            }
            writer.EndObject();
        }
        private static string GetTemporaryFileName()
        {
            string directory = GetTemporaryDirectory();
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            return Path.Combine(directory, "journal_" + Guid.NewGuid().ToString("N") + ".py");
        }
        // A name that is no file, or not a Python one, is refused before the journal is written anywhere
        private static void AssertJournalFileName(string journalFile)
        {
            if (journalFile.Trim().Length == 0)
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument journal_file is empty. Name the .py " +
                                                                   "file to write the journal to, or leave it out " +
                                                                   "to have the journal returned inline. Nothing " +
                                                                   "was done.");
            string extension;
            try
            {
                extension = Path.GetExtension(journalFile);
            }
            catch (ArgumentException exception)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument journal_file is not a valid file " +
                                                                   "name: " + exception.Message + " Nothing was done.");
            }
            if (extension == null || extension.ToLower() != ".py")
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument journal_file must name a .py file, " +
                                                                   "because the journal is a Python script. " +
                                                                   "Nothing was done.");
        }
        // A relative name is placed where a screenshot is, not in whatever folder the process started in, and
        // a folder the name points into is created when it is missing, the way a screenshot's is
        private static string GetRequestedFileName(string requestedFile)
        {
            string fileName = Path.IsPathRooted(requestedFile) ? requestedFile
                                                               : Path.Combine(GetTemporaryDirectory(), requestedFile);
            string directory = Path.GetDirectoryName(fileName);
            try
            {
                if (directory != null && directory.Length > 0 && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
            catch (Exception exception)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The directory " + directory +
                                                                   " does not exist and could not be created: " +
                                                                   exception.Message);
            }
            return fileName;
        }
        private static string GetTemporaryDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "PrePoMax");
        }
        private static string ReadAndRemove(string fileName)
        {
            try
            {
                if (!File.Exists(fileName)) return null;
                string text = File.ReadAllText(fileName);
                if (text.Length <= InlineJournalLimit) File.Delete(fileName);
                return text;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
