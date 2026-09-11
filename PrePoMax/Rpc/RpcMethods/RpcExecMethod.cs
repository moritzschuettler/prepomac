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
using PrePoMax.Python;
using System;
using System.Collections.Generic;

namespace PrePoMax.Rpc
{
    // Runs Python against the live model, through the same engine the Python panel runs. Two ways,
    // both of which the panel already offers the user:
    //
    // "commands" - the default - records what the source did as the individual commands a user would
    // have produced by hand, so the history stays editable, regenerable and journalable.
    //
    // "script" - records the source as one command, the way --script does. Regeneration then re-runs
    // the source, which is what keeps a computed model parametric, but the variables do not survive
    // the call because a recorded script gets a scope of its own.
    public static class RpcExecMethod
    {
        // Variables                                                                                                                
        private static readonly string[] RecordModes = { "commands", "script" };


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            string source = request.RequireString("source");
            string recordMode = request.Has("record") ? request.RequireOneOf("record", RecordModes) : RecordModes[0];
            Controller controller = server.Controller;
            if (controller == null) throw new RpcException(RpcErrorCode.InternalError, "There is no open model.");
            //
            server.Form.PrepareAutomationPythonEngine();
            int commandsBefore = GetCommandCount(controller);
            DateTime started = DateTime.Now;
            PythonEngineResult result;
            if (recordMode == "script") result = controller.RunPythonScriptCommand(source);
            else result = PythonEngineManager.ExecuteScript(source);
            double elapsed = (DateTime.Now - started).TotalSeconds;
            List<Command> recorded = GetRecordedCommands(controller, commandsBefore);
            //
            server.Form.EchoAutomationExecution("Automation server - exec", source, result.Output, result.Error);
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("record", recordMode);
            writer.WriteValue("output", result.Output == null ? "" : result.Output);
            writer.WriteValue("error", result.Error == null ? "" : result.Error);
            writer.WriteValue("failed", !string.IsNullOrEmpty(result.Error));
            writer.WriteValue("elapsed", elapsed);
            WriteRecordedCommands(writer, recorded);
            writer.EndObject();
            return writer.ToString();
        }
        public static int GetCommandCount(Controller controller)
        {
            List<Command> commands = controller.GetCommands();
            return commands == null ? 0 : commands.Count;
        }
        // The commands the call added to the history, in the order they were recorded. This is what
        // makes an agent's work visible as ordinary user actions: the user can delete any of them in
        // the command history editor and regenerate.
        //
        // Each one is reported as its class name and as the line the command history editor shows,
        // which names the item. A call that writes several items is otherwise a list of types that
        // says how many things happened but not to what
        public static List<Command> GetRecordedCommands(Controller controller, int countBefore)
        {
            List<Command> recorded = new List<Command>();
            List<Command> commands = controller.GetCommands();
            if (commands == null) return recorded;
            //
            for (int i = countBefore; i < commands.Count; i++) recorded.Add(commands[i]);
            return recorded;
        }
        public static void WriteRecordedCommands(JsonWriter writer, List<Command> recorded)
        {
            writer.BeginArray("recordedCommands");
            for (int i = 0; i < recorded.Count; i++)
            {
                Command command = recorded[i];
                writer.BeginObject();
                writer.WriteValue("type", command == null ? "" : command.GetType().Name);
                writer.WriteValue("description", GetDescription(command));
                writer.EndObject();
            }
            writer.EndArray();
        }
        // The command history line without the timestamp it opens with, which is noise to a caller
        // that already knows when it made the call
        public static string GetDescription(Command command)
        {
            if (command == null) return "";
            //
            string text = command.GetCommandString();
            if (text == null) return "";
            if (command.Name == null || command.Name.Length == 0) return text.Trim();
            //
            int index = text.IndexOf(command.Name, StringComparison.Ordinal);
            return (index < 0 ? text : text.Substring(index)).Trim();
        }
    }
}
