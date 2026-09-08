// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrePoMaxMcp
{
    // The tools, and what each one sends to the automation server. The list is deliberately short:
    // the model surface is the Python API, which an agent writes directly through run_python and
    // learns through describe_api, so a tool per API member would be both impossible to list and a
    // second front door free to drift from the first.
    //
    // A tool that is not a protocol primitive is a piece of Python this bridge sends - read_model is
    // one - so a new tool costs a string here and no change to the application.
    public static class McpToolCatalog
    {
        // Variables                                                                                                                
        private const string NoArguments = "{\"type\": \"object\", \"properties\": {}}";
        private static List<McpTool> _tools;


        // Static methods                                                                                                           
        public static List<McpTool> GetTools()
        {
            if (_tools != null) return _tools;
            //
            _tools = new List<McpTool>();
            _tools.Add(new McpTool("list_sessions", "List PrePoMax sessions",
                "Lists the running PrePoMax instances that have their automation server on. Call this first when a " +
                "tool reports that there is no session, or when the user has more than one PrePoMax open. Every " +
                "other tool takes the process id of the session it should act on; with exactly one session running " +
                "it may be left out.",
                NoArguments, true));
            _tools.Add(new McpTool("get_status", "Read the session state",
                "Reports what PrePoMax is doing right now: whether it is idle or busy and why, the open model file, " +
                "the current view, the unit system, how many commands are in the history, and whether the server is " +
                "in read only mode. Call it when a tool answers that the application is busy, and before a change, " +
                "so the state the change assumes is the state it finds.",
                SessionSchema(null), true));
            _tools.Add(new McpTool("describe_api", "Learn the Python API",
                "Describes a part of the PrePoMax Python API, read off the API itself, so it is always current. " +
                "'pmx' is the root object; every path below it is described the same way, and a collection also " +
                "reports the item names it holds right now. This is how to find the right call before writing it: " +
                "start at 'pmx', then 'pmx.model', then the collection the work belongs to.",
                SessionSchema("\"path\": {\"type\": \"string\", \"description\": " +
                              "\"The Python path to describe, for example pmx, pmx.model.steps or " +
                              "pmx.model.steps['Step-1'].loads. Defaults to pmx.\"}"), true));
            _tools.Add(new McpTool("read_model", "Read the model",
                "Returns a snapshot of the open model as JSON: the model properties, the parts with their mesh " +
                "state, the item names of every collection, the steps with what they contain, and the analyses " +
                "with their status. Nothing is changed. This is the cheapest way to see what is in the model " +
                "before deciding what to do to it.",
                SessionSchema(null), true));
            _tools.Add(new McpTool("eval_expression", "Evaluate an expression",
                "Evaluates one Python expression against the live model and returns its value, both as JSON when " +
                "it has a JSON form and as its repr. Use it for reading - a name list, a property, a computed " +
                "number. It is not meant to change the model; use run_python for that.",
                SessionSchema("\"expression\": {\"type\": \"string\", \"description\": " +
                              "\"One Python expression, for example pmx.model.parts.get_names().\"}",
                              "\"expression\""), true));
            _tools.Add(new McpTool("run_python", "Run Python in the session",
                "Runs Python against the model the user has open, through the same engine the Python panel in " +
                "PrePoMax runs. Every change is recorded in the command history as the command the user would have " +
                "produced by hand, so the user can see it in the tree, undo it in Edit - Command history, and " +
                "regenerate. Write the API calls directly; use describe_api to find them. The variables of one " +
                "call survive into the next, because the scope is the one the Python console uses.\n\n" +
                "record: 'commands' (the default) records each API call as its own command. 'script' records the " +
                "whole source as one command instead, which is what keeps a computed model parametric - " +
                "regeneration re-runs the source - but the variables do not survive the call.\n\n" +
                "If the answer says the application is busy, the user is editing something. Wait and try again " +
                "rather than working around it.",
                SessionSchema("\"source\": {\"type\": \"string\", \"description\": " +
                              "\"The Python source to run. More than one line is fine.\"}, " +
                              "\"record\": {\"type\": \"string\", \"enum\": [\"commands\", \"script\"], " +
                              "\"description\": \"How the source is recorded in the command history. " +
                              "Defaults to commands.\"}",
                              "\"source\""), false));
            _tools.Add(new McpTool("get_history", "Read the command history",
                "Returns the command history of the session and, on request, the journal of it: the whole session " +
                "written as the Python script a user would have written. The journal is the best way to learn what " +
                "the user did by hand - it names the exact API calls - and the best way to hand the session back " +
                "as a script.",
                SessionSchema("\"include_journal\": {\"type\": \"boolean\", \"description\": " +
                              "\"Emit the history as a Python script as well. Defaults to false.\"}, " +
                              "\"limit\": {\"type\": \"integer\", \"description\": " +
                              "\"How many of the most recent commands to list. 0 lists all of them. " +
                              "Defaults to 200.\"}"), true));
            _tools.Add(new McpTool("run_job", "Run an analysis",
                "Writes the solver input file and starts the analysis, without waiting for it. The solver runs " +
                "while the user keeps working, and PrePoMax shows its own monitor. Poll get_job for the status, " +
                "and open the results with run_python when it reports OK.",
                SessionSchema("\"name\": {\"type\": \"string\", \"description\": " +
                              "\"The name of the analysis to run.\"}", "\"name\""), false));
            _tools.Add(new McpTool("get_job", "Read an analysis status",
                "Reports the status of an analysis: None, InQueue, Running, OK, Killed, TimedOut, Failed or " +
                "FailedWithResults, and on request everything the solver wrote to its output.",
                SessionSchema("\"name\": {\"type\": \"string\", \"description\": " +
                              "\"The name of the analysis.\"}, " +
                              "\"include_output\": {\"type\": \"boolean\", \"description\": " +
                              "\"Return the solver output as well. Defaults to false.\"}", "\"name\""), true));
            _tools.Add(new McpTool("screenshot", "Picture the viewport",
                "Writes a PNG of the 3D viewport as it looks right now and returns the path. Use it to see what " +
                "the user sees - a picture settles a question about orientation, a selection or a result plot that " +
                "no amount of reading the model can.",
                SessionSchema("\"file_name\": {\"type\": \"string\", \"description\": " +
                              "\"Where to write the .png. Defaults to a file in the temporary folder.\"}"), true));
            return _tools;
        }
        public static string GetInstructions()
        {
            StringBuilder text = new StringBuilder();
            text.Append("These tools drive a running PrePoMax session - the one the user has open on their own ");
            text.Append("desktop. The model changes in front of them while you work, so treat it as their model: ");
            text.Append("read it before you change it, and change it the way they asked.\n\n");
            text.Append("The model surface is the PrePoMax Python API. 'pmx' is its root object, describe_api is ");
            text.Append("how to learn it, and run_python is how to use it. Look a call up rather than guessing: ");
            text.Append("the API refuses what it does not recognize, and the refusal names the reason.\n\n");
            text.Append("Everything you change is recorded in the command history as the command the user would ");
            text.Append("have produced by hand. They can see it in the model tree, remove it in ");
            text.Append("Edit - Command history and regenerate, so a mistake is recoverable - but it is still ");
            text.Append("their session, so do not save over their file or close their application.\n\n");
            text.Append("A tool that answers that the application is busy means the user is doing something - a ");
            text.Append("dialog is open, or a mesher is running. Nothing was done. Wait and try the same call ");
            text.Append("again rather than working around it.");
            return text.ToString();
        }
        public static McpTool Find(string name)
        {
            foreach (McpTool tool in GetTools())
            {
                if (tool.Name == name) return tool;
            }
            return null;
        }
        // Sends what the tool asks for and returns the text of the answer
        public static string Execute(string name, object arguments, PipeClient client)
        {
            if (name == "get_status") return CallAndPrint(client, "status", null);
            if (name == "describe_api")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                string path = JsonReader.GetString(arguments, "path", null);
                if (path != null) parameters.Add("path", path);
                return CallAndPrint(client, "describe", parameters);
            }
            // The snapshot is what the script printed, so it is returned as it was printed rather
            // than wrapped in the answer of the call that ran it
            if (name == "read_model") return CallAndReadOutput(client, McpModelScript.Source);
            if (name == "eval_expression")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("expression", RequireString(arguments, "expression"));
                return CallAndPrint(client, "eval", parameters);
            }
            if (name == "run_python")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("source", RequireString(arguments, "source"));
                string record = JsonReader.GetString(arguments, "record", null);
                if (record != null) parameters.Add("record", record);
                return CallAndPrint(client, "exec", parameters);
            }
            if (name == "get_history")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("include_journal", JsonReader.GetBool(arguments, "include_journal", false));
                if (JsonReader.HasItem(arguments, "limit"))
                    parameters.Add("limit", JsonReader.GetInt(arguments, "limit", 200));
                return CallAndPrint(client, "history", parameters);
            }
            if (name == "run_job")
            {
                string jobName = RequireString(arguments, "name");
                string source = "pmx.jobs[" + Quote(jobName) + "].run(wait=False)";
                return CallAndPrint(client, "exec", ExecArguments(source));
            }
            if (name == "get_job")
            {
                string jobName = RequireString(arguments, "name");
                bool includeOutput = JsonReader.GetBool(arguments, "include_output", false);
                string expression = includeOutput
                                    ? "{'status': pmx.jobs[" + Quote(jobName) + "].status, 'output': pmx.jobs[" +
                                      Quote(jobName) + "].get_output()}"
                                    : "{'status': pmx.jobs[" + Quote(jobName) + "].status, 'isUpToDate': " +
                                      "pmx.jobs[" + Quote(jobName) + "].is_up_to_date}";
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("expression", expression);
                return CallAndPrint(client, "eval", parameters);
            }
            if (name == "screenshot")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                string fileName = JsonReader.GetString(arguments, "file_name", null);
                if (fileName != null) parameters.Add("file_name", fileName);
                return CallAndPrint(client, "screenshot", parameters);
            }
            throw new McpException("There is no tool named " + name + ".");
        }
        private static string CallAndReadOutput(PipeClient client, string source)
        {
            object answer = client.Call("exec", ExecArguments(source));
            Dictionary<string, object> error = JsonReader.GetObject(answer, "error");
            if (error != null)
            {
                throw new McpException(JsonReader.GetString(error, "name", "Error") + ": " +
                                       JsonReader.GetString(error, "message", ""));
            }
            object result = JsonReader.GetObject(answer, "result");
            if (JsonReader.GetBool(result, "failed", false))
                throw new McpException(JsonReader.GetString(result, "error", "The script failed."));
            return JsonReader.GetString(result, "output", "");
        }
        private static Dictionary<string, object> ExecArguments(string source)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("source", source);
            return parameters;
        }
        private static string CallAndPrint(PipeClient client, string method, Dictionary<string, object> parameters)
        {
            object answer = client.Call(method, parameters);
            Dictionary<string, object> error = JsonReader.GetObject(answer, "error");
            if (error != null)
            {
                string errorName = JsonReader.GetString(error, "name", "Error");
                string message = JsonReader.GetString(error, "message", "");
                throw new McpException(errorName + ": " + message);
            }
            object result = JsonReader.GetObject(answer, "result");
            if (result == null) return "{}";
            string text = JsonValuePrinter.Write(result);
            // Python that failed is reported as a failed tool call, with the whole answer as the
            // text: the traceback is in it, and so is what was recorded before it failed
            if (JsonReader.GetBool(result, "failed", false)) throw new McpException(text);
            return text;
        }
        private static string RequireString(object arguments, string name)
        {
            string value = JsonReader.GetString(arguments, name, null);
            if (string.IsNullOrEmpty(value)) throw new McpException("The argument " + name + " is required.");
            return value;
        }
        private static string Quote(string value)
        {
            return "'" + value.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
        }
        // Every tool may name the session it acts on, because the user may have more than one PrePoMax open
        private static string SessionSchema(string properties)
        {
            return SessionSchema(properties, null);
        }
        private static string SessionSchema(string properties, string required)
        {
            StringBuilder schema = new StringBuilder();
            schema.Append("{\"type\": \"object\", \"properties\": {");
            if (properties != null) schema.Append(properties).Append(", ");
            schema.Append("\"process_id\": {\"type\": \"integer\", \"description\": ");
            schema.Append("\"The process id of the PrePoMax session to act on, from list_sessions. ");
            schema.Append("Only needed when more than one is running.\"}}");
            if (required != null) schema.Append(", \"required\": [").Append(required).Append("]");
            schema.Append("}");
            return schema.ToString();
        }
    }
}
