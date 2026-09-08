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
            if (limit < 0) limit = DefaultLimit;
            int first = limit == 0 || limit >= commands.Count ? 0 : commands.Count - limit;
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
                writer.EndObject();
            }
            writer.EndArray();
            //
            if (request.GetBool("include_journal", false)) WriteJournal(writer, request, commands);
            writer.EndObject();
            return writer.ToString();
        }
        private static void WriteJournal(JsonWriter writer, RpcRequest request, List<Command> commands)
        {
            string requestedFile = request.GetString("journal_file", null);
            string fileName = requestedFile == null ? GetTemporaryFileName() : requestedFile;
            PythonJournalReport report = PythonJournalExporter.Export(commands, fileName, "the automation server");
            //
            writer.BeginObject("journal");
            writer.WriteValue("emittedCommandCount", report.EmittedCommandCount);
            writer.WriteValue("refusedCommandCount", report.RefusedCommandCount);
            writer.BeginArray("refusals");
            foreach (string refusal in report.Refusals) writer.WriteValue(refusal);
            writer.EndArray();
            //
            string text = ReadAndRemove(fileName, requestedFile == null);
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
            string directory = Path.Combine(Path.GetTempPath(), "PrePoMax");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            return Path.Combine(directory, "journal_" + Guid.NewGuid().ToString("N") + ".py");
        }
        private static string ReadAndRemove(string fileName, bool remove)
        {
            try
            {
                if (!File.Exists(fileName)) return null;
                string text = File.ReadAllText(fileName);
                if (remove && text.Length <= InlineJournalLimit) File.Delete(fileName);
                return text;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
