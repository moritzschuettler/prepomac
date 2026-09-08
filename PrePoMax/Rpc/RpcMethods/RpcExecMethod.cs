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
            List<string> recorded = GetRecordedCommands(controller, commandsBefore);
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
        // the command history editor and regenerate
        public static List<string> GetRecordedCommands(Controller controller, int countBefore)
        {
            List<string> names = new List<string>();
            List<Command> commands = controller.GetCommands();
            if (commands == null) return names;
            //
            for (int i = countBefore; i < commands.Count; i++)
            {
                Command command = commands[i];
                names.Add(command == null ? "" : command.GetType().Name);
            }
            return names;
        }
        public static void WriteRecordedCommands(JsonWriter writer, List<string> recorded)
        {
            writer.BeginArray("recordedCommands");
            for (int i = 0; i < recorded.Count; i++) writer.WriteValue(recorded[i]);
            writer.EndArray();
        }
    }
}
