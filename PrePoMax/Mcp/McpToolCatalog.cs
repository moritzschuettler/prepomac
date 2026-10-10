// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Python.Journal;
using PrePoMax.Rpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PrePoMax.Mcp
{
    // The tools, and what each one asks the automation server for. The list is deliberately short:
    // the model surface is the Python API, which an agent writes directly through run_python and
    // learns through describe_api, so a tool per API member would be both impossible to list and a
    // second front door free to drift from the first.
    //
    // A tool that is not a protocol primitive is a piece of Python the catalog holds - read_model is
    // one - so a new tool costs a string here and no change to the application.
    public static class McpToolCatalog
    {
        // Variables                                                                                                                
        private const string NoArguments = "{\"type\": \"object\", \"properties\": {}, \"additionalProperties\": false}";
        private const int DefaultTopologyLimit = 40;
        // The whole answer lists no more than this many faces, edges and vertices together: each part
        // lists up to its limit of each kind, out of what is left
        private const int TopologyRowBudget = 240;
        // The bound of every timeout argument, which the dispatcher checks as well
        private static readonly string TimeoutRange = "\"type\": \"integer\", \"minimum\": 0, \"maximum\": " +
                                                       RpcDispatcher.MaximumWaitSeconds + ", ";
        private static List<McpTool> _tools;
        // How the answer of a call a tool started is shaped when get_call collects it, by call id, latest last.
        // A call id is unique to the dispatcher that issued it, which is kept beside it, because a restarted
        // server numbers its calls from one again
        private const int MaximumShapedCalls = 64;
        private static readonly List<Tuple<RpcDispatcher, int, Func<string, string>>> _shapedCalls =
            new List<Tuple<RpcDispatcher, int, Func<string, string>>>();


        // Static methods                                                                                                           
        public static List<McpTool> GetTools()
        {
            if (_tools != null) return _tools;
            //
            List<McpTool> tools = new List<McpTool>();
            tools.Add(new McpTool("get_guide", "Read the driving guide",
                "How to drive a PrePoMax session: the order the work goes in, what a number without a unit " +
                "means, how to find the face you meant before you load it, how to run an analysis and read it " +
                "back, and what to do when a call is refused. It is served by the application, so it is the " +
                "guide of this version. Read it before the first change of a session.",
                NoArguments, true));
            tools.Add(new McpTool("list_sessions", "List PrePoMax sessions",
                "Lists the running PrePoMax instances that have their automation server on, each with the URL it " +
                "answers on, and marks the one this connection reaches. Every tool here acts on that one session - " +
                "the URL is the address, so there is nothing to switch. Call this when the user has more than one " +
                "PrePoMax open, and tell them which URL to point the client at; do not try to reach another " +
                "session yourself.",
                NoArguments, true));
            tools.Add(new McpTool("get_status", "Read the session state",
                "Reports what PrePoMax is doing right now: whether it is idle or busy and why, the open model file, " +
                "the current view, the unit system (model.unitSystem), how many commands are in the history, " +
                "whether the server is in read only mode, whether Regenerate is replaying the history " +
                "(isRegenerating) or a recorded script keeps what runs out of the history " +
                "(isCommandRecordingSuppressed), and under calls the long calls still running or waiting " +
                "to be collected. Call it when a tool answers that the application is busy, and before a change, " +
                "so the state the change assumes is the state it finds.",
                SessionSchema(null), true));
            tools.Add(new McpTool("describe_api", "Learn the Python API",
                "Describes a part of the PrePoMax Python API, read off the API itself, so it is always current. " +
                "'pmx' is the root object; every path below it is described the same way, and a collection also " +
                "reports the item names it holds right now. A path that ends at a method or a property describes " +
                "that member with its parameters, and a mistyped path is answered with the likely paths. This is " +
                "how to find the right call before writing it: start at 'pmx', then 'pmx.model', then the " +
                "collection the work belongs to.",
                SessionSchema("\"path\": {\"type\": \"string\", \"description\": " +
                              "\"The Python path to describe, for example pmx, pmx.model.steps or " +
                              "pmx.model.steps['Step-1'].loads. It may also be an API type name, for " +
                              "example Loads, to read a collection before anything holding it exists, or a " +
                              "type name followed by a member path, for example Result.mesh, to read the " +
                              "results side before a result is open. Defaults to pmx.\"}"), true));
            tools.Add(new McpTool("read_model", "Read the model",
                "Returns a snapshot of the open model as JSON: the model properties, the parts with their mesh " +
                "state - whether each one is meshed, with its node and element count - the item names of every " +
                "collection, the mesh setup items, how many user keywords and keyword edits there are, the " +
                "parameters with their equation, value and " +
                "description, the steps with what they contain, and the analyses with their status. Nothing is " +
                "changed. This is the cheapest way to see what is in the model before deciding what to do to it.",
                SessionSchema(null), true));
            tools.Add(new McpTool("read_topology", "Read the part geometry",
                "Returns the faces, edges and vertices of the parts as JSON, each with the ID a selection " +
                "takes, its size, its centroid and - for a face - its normal. This is how to find the entity " +
                "you mean without seeing the viewport: the top face of a box is the one whose normal points " +
                "up. Nothing is changed.\n\n" +
                "Every part is summarized with its counts and its bounding box. The faces of a part are listed " +
                "largest first, its edges longest first and its vertices in index order, each kind capped at " +
                "limit per part, and the whole answer lists no more than " + TopologyRowBudget + " rows " +
                "together, so on an assembly the first parts are detailed and the rest say to ask for them by " +
                "name. " +
                "Read the remainder of one part through get_face_data and get_edge_data on it. Part names are " +
                "compared regardless of case but never trimmed, and a name that matches no part - a padded one " +
                "included - is reported under unmatched_part_names.",
                SessionSchema("\"part_names\": {\"type\": \"array\", \"items\": {\"type\": \"string\"}, " +
                              "\"description\": \"The parts to read, at least one. All of them when this is " +
                              "left out.\"}, " +
                              "\"limit\": {\"type\": \"integer\", \"minimum\": 1, \"maximum\": " +
                              TopologyRowBudget + ", \"description\": " +
                              "\"How many faces, edges and vertices to list per part, 1 to " + TopologyRowBudget +
                              ". Defaults to " + DefaultTopologyLimit + ".\"}"), true));
            tools.Add(new McpTool("eval_expression", "Evaluate an expression",
                "Evaluates one Python expression against the live model and returns its value, both as JSON when " +
                "it has a JSON form and as its repr. Use it for reading - a name list, a property, a computed " +
                "number. It is not meant to change the model; use run_python for that. An expression that takes " +
                "longer than the wait is answered the way run_python answers a long call.",
                SessionSchema("\"expression\": {\"type\": \"string\", \"description\": " +
                              "\"One Python expression, for example pmx.model.parts.get_names().\"}, " +
                              "\"timeout\": {" + TimeoutRange + "\"description\": " +
                              "\"How many seconds to wait for the answer, 0 to 600. Defaults to the " +
                              "requestTimeout get_status reports. A call that takes longer is not " +
                              "stopped: it is answered with running and a callId, and get_call collects " +
                              "its answer. The wait for another call to finish first counts in it, and a " +
                              "call that cannot start within it, or within the requestTimeout, is refused " +
                              "as busy. Keep it below the " +
                              "request timeout of your own client, or pass 0 and wait through get_call for " +
                              "long work.\"}",
                              "\"expression\""), false));
            tools.Add(new McpTool("run_python", "Run Python in the session",
                "Runs Python against the model the user has open, through the same engine the Python panel in " +
                "PrePoMax runs. Every change is recorded in the command history as the command the user would have " +
                "produced by hand, so the user can see it in the tree, remove it in Edit - Edit History, and " +
                "rebuild the model in Edit - Regenerate History. Write the API calls directly; use describe_api " +
                "to find them. The variables of one call survive into the next, because the scope is the one the " +
                "Python console uses.\n\n" +
                "record: 'commands' (the default) records each API call as its own command. 'script' records the " +
                "whole source as one command instead, which is what keeps a computed model parametric - " +
                "regeneration re-runs the source - but the variables do not survive the call. A script that fails " +
                "after it changed the model is still recorded, so the history holds what it did up to the error.\n\n" +
                "A long operation - meshing a large part, an import, a regeneration - may outlast the wait, which " +
                "is the requestTimeout get_status reports unless timeout says otherwise. It is not stopped and not " +
                "lost: the answer is {\"running\": true, \"callId\": n}, the call runs to the end and records " +
                "its commands as usual, and get_call with that callId returns the answer this tool would have " +
                "returned. Until then every other tool waits for it and is refused as busy after the " +
                "requestTimeout - all but get_status, get_call, cancel_call, get_guide and list_sessions - and " +
                "get_status names it. The wait for such a call counts against timeout: a run_python that is " +
                "still waiting for its turn when timeout runs out is refused as busy and does not run, at once " +
                "for a timeout of 0.\n\n" +
                "If the answer says the application is busy, the user is editing something. Wait and try again " +
                "rather than working around it.",
                SessionSchema("\"source\": {\"type\": \"string\", \"description\": " +
                              "\"The Python source to run. More than one line is fine.\"}, " +
                              "\"record\": {\"type\": \"string\", \"enum\": [\"commands\", \"script\"], " +
                              "\"description\": \"How the source is recorded in the command history. " +
                              "Defaults to commands.\"}, " +
                              "\"timeout\": {" + TimeoutRange + "\"description\": " +
                              "\"How many seconds to wait for the answer, 0 to 600. Defaults to the " +
                              "requestTimeout get_status reports. A call that takes longer is not " +
                              "stopped: it is answered with running and a callId, and get_call collects " +
                              "its answer. The wait for another call to finish first counts in it, and a " +
                              "call that cannot start within it, or within the requestTimeout, is refused " +
                              "as busy. Keep it below the " +
                              "request timeout of your own client, or pass 0 and wait through get_call for " +
                              "long work.\"}",
                              "\"source\""), false));
            tools.Add(new McpTool("get_call", "Collect a long call",
                "Collects the answer of a call that was still running when its wait ran out and was answered " +
                "with running and a callId - by run_python or eval_expression, or by read_model, read_topology, " +
                "run_job or get_job, which run Python as well. It waits up to timeout seconds and returns the " +
                "answer the tool that started the call would have returned - for run_python its output, its " +
                "error and the commands it recorded, for read_model the snapshot - or, if it is still working, " +
                "running and the callId once more; ask again. An answer is given up " +
                "once its response has been written to the client; one whose response the client gave up on or " +
                "cancelled stays collectable, the latest 16 for an hour. get_status lists the calls that are " +
                "running or waiting to be collected.",
                SessionSchema("\"call_id\": {\"type\": \"integer\", \"minimum\": 1, \"description\": " +
                              "\"The callId the still running call was answered with.\"}, " +
                              "\"timeout\": {" + TimeoutRange + "\"description\": " +
                              "\"How many seconds to wait for it, 0 to 600. Defaults to the requestTimeout " +
                              "get_status reports. Keep it below the request timeout of your own client.\"}",
                              "\"call_id\""), true));
            tools.Add(new McpTool("cancel_call", "Stop a long call",
                "Stops a call that was answered with running and a callId - by run_python, eval_expression or " +
                "another tool that runs Python - one that loops forever or is doing more than was meant. It is " +
                "answered within a few seconds, also while that call holds the session, and in read only mode, " +
                "because it changes nothing in the model itself.\n\n" +
                "The call is interrupted at its next Python statement and ends with a KeyboardInterrupt error. " +
                "An operation of PrePoMax it is inside - meshing, an import, a regeneration - cannot be " +
                "interrupted and finishes first, and a blocking wait - time.sleep, a socket, input() - is not " +
                "interrupted until it returns, so the answer may say it has not stopped yet. Code that catches the " +
                "interrupt - a bare except - keeps running: cancel it again, since every cancel delivers a new " +
                "interrupt. The answer says only what happened: isInterrupted when the interrupt was delivered, " +
                "isInterruptPending while it waits - for such an operation to return, or for a call that has not " +
                "reached its Python yet, which then does not run it and ends with a KeyboardInterrupt error or " +
                "with the refusal it meets first, such as busy. The commands it recorded " +
                "before it stopped stay in the command history. Collect its answer with get_call afterwards: it " +
                "lists them.",
                SessionSchema("\"call_id\": {\"type\": \"integer\", \"minimum\": 1, \"description\": " +
                              "\"The callId the still running call was answered with.\"}",
                              "\"call_id\""), false));
            tools.Add(new McpTool("get_history", "Read the command history",
                "Returns the command history of the session and, on request, the journal of it: the whole session " +
                "written as the Python script a user would have written. The journal is the best way to learn what " +
                "the user did by hand - it names the exact API calls - and the best way to hand the session back " +
                "as a script. A journal longer than 200,000 characters is not returned inline: it is written " +
                "to a file in the temporary folder, and the answer gives its path as journal.fileName.",
                SessionSchema("\"include_journal\": {\"type\": \"boolean\", \"description\": " +
                              "\"Emit the history as a Python script as well. Defaults to false.\"}, " +
                              "\"limit\": {\"type\": \"integer\", \"minimum\": 0, \"description\": " +
                              "\"How many of the most recent commands to list. 0 lists all of them. " +
                              "Defaults to 200.\"}"), true));
            tools.Add(new McpTool("run_job", "Run an analysis",
                "Writes the solver input file and starts the analysis, without waiting for it, and returns the " +
                "status it started with. The solver runs while the user keeps working; no monitor is opened " +
                "for it. Poll get_job for the status, and open the results with run_python when it reports OK. " +
                "If the user opens the monitor of the analysis, it is a dialog like any other: every tool but " +
                "get_status, get_call, cancel_call, get_guide and list_sessions is refused as busy until it is " +
                "closed.",
                SessionSchema("\"name\": {\"type\": \"string\", \"description\": " +
                              "\"The name of the analysis to run.\"}", "\"name\""), false));
            tools.Add(new McpTool("get_job", "Read an analysis status",
                "Reports the status of an analysis - None, InQueue, Running, OK, Killed, TimedOut, Failed or " +
                "FailedWithResults - and isUpToDate, which is false when the model changed after the run, and " +
                "on request everything the solver wrote to its output.",
                SessionSchema("\"name\": {\"type\": \"string\", \"description\": " +
                              "\"The name of the analysis.\"}, " +
                              "\"include_output\": {\"type\": \"boolean\", \"description\": " +
                              "\"Return the solver output as well. Defaults to false.\"}", "\"name\""), true));
            tools.Add(new McpTool("screenshot", "Picture the viewport",
                "Writes a PNG of the 3D viewport, returns the path and attaches the picture as an image, " +
                "unless it is larger than 4 MB. Use it to see what the user sees - a " +
                "picture settles a question about orientation, a selection or a result plot that no amount of " +
                "reading the model can.\n\n" +
                "The optional arguments set up the shot before it is taken, through pmx.view, and are the same " +
                "calls you could send with run_python. They move the user's view and it stays moved, so ask " +
                "for what you need and no more. None of it is recorded in the command history, because none " +
                "of it changes the model.",
                SessionSchema("\"file_name\": {\"type\": \"string\", \"description\": " +
                              "\"Where to write the .png. Defaults to a new file in the PrePoMax folder of " +
                              "the temporary folder, and a relative name is taken in that folder too.\"}, " +
                              "\"transparent_background\": {\"type\": \"boolean\", \"description\": " +
                              "\"Leave the viewport background out of the picture, so it can be put on a page " +
                              "of any color. Defaults to false, which pictures the background the user sees.\"}, " +
                              "\"magnification\": {\"type\": \"integer\", \"minimum\": 1, \"maximum\": 4, " +
                              "\"description\": " +
                              "\"Render the picture this many times larger than the viewport, 1 to 4. Defaults " +
                              "to 1. It is for a picture that is going to be printed, not for reading a label: " +
                              "the model is drawn at the larger size, but the legend, the scale bar and the " +
                              "triad keep the size they have on screen, so they come out smaller.\"}, " +
                              "\"view\": {\"type\": \"string\", \"enum\": [\"geometry\", \"femodel\", " +
                              "\"results\"], \"description\": \"The tree to display first.\"}, " +
                              "\"orientation\": {\"type\": \"string\", \"enum\": [\"Front\", \"Back\", " +
                              "\"Top\", \"Bottom\", \"Left\", \"Right\", \"Isometric\", " +
                              "\"IsometricNegative\", \"Normal\", \"Vertical\"], \"description\": " +
                              "\"Where to put the camera.\"}, " +
                              "\"zoom_to_fit\": {\"type\": \"boolean\", \"description\": " +
                              "\"Fit everything visible in the viewport first.\"}, " +
                              "\"field\": {\"type\": \"string\", \"description\": " +
                              "\"The result field to plot, for example DISP. Needs an open result.\"}, " +
                              "\"component\": {\"type\": \"string\", \"description\": " +
                              "\"The component of that field, for example U3. Defaults to the first one. " +
                              "Needs field.\"}"),
                false));
            tools.Add(new McpTool("exit_session", "Close a hidden session",
                "Closes this PrePoMax, and only when it runs without a window - a session started with -g No " +
                "and the automation server, which nobody is looking at and which has no other way to end. A " +
                "session open on the user's desktop is refused: the user closes their own application. The " +
                "model is not saved; one with unsaved changes is refused unless discard_changes is true, so " +
                "save it with pmx.save(file_name) first when the work should be kept.",
                SessionSchema("\"discard_changes\": {\"type\": \"boolean\", \"description\": " +
                              "\"Close although the model has unsaved changes, which are lost. Defaults to " +
                              "false.\"}"), false));
            _tools = tools;
            return _tools;
        }
        public static string GetInstructions()
        {
            StringBuilder text = new StringBuilder();
            text.Append("These tools drive a running PrePoMax session - the one the user has open on their own ");
            text.Append("desktop. The model changes in front of them while you work, so treat it as their model: ");
            text.Append("read it before you change it, and change it the way they asked.\n\n");
            text.Append("Call get_guide before the first change of a session. It is short, it is served by ");
            text.Append("this application so it matches this version, and it says how the work is ordered and ");
            text.Append("where a scripted model usually goes wrong.\n\n");
            text.Append("The model surface is the PrePoMax Python API. 'pmx' is its root object, describe_api is ");
            text.Append("how to learn it, and run_python is how to use it. Look a call up rather than guessing: ");
            text.Append("the API refuses what it does not recognize, and the refusal names the reason.\n\n");
            text.Append("A plain number is a value in the model's unit system, which get_status reports as ");
            text.Append("model.unitSystem. Nothing converts it and nothing warns you, so read the unit system before ");
            text.Append("you write the first number: 210000 is a steel modulus in MM_TON_S_C and wrong in ");
            text.Append("M_KG_S_C. Where the user could type an expression you may pass one as a string, and ");
            text.Append("it is stored as an equation that re-evaluates.\n\n");
            text.Append("Everything you change is recorded in the command history as the command the user would ");
            text.Append("have produced by hand. They can see it in the model tree, remove it in ");
            text.Append("Edit - Edit History and rebuild the model in Edit - Regenerate History, so a mistake is ");
            text.Append("recoverable - but it is still ");
            text.Append("their session, so do not save over their file or close their application.\n\n");
            text.Append("A tool that answers that the application is busy means the user is doing something - a ");
            text.Append("dialog is open, or a mesher is running. Nothing was done. Wait and try the same call ");
            text.Append("again rather than working around it.\n\n");
            text.Append("A call that outlasts its wait - meshing a large part, an import, a regeneration, a ");
            text.Append("long expression - is answered with running and a callId. It is not stopped: it finishes and ");
            text.Append("records its commands as usual, and get_call with that callId returns its answer. Do not ");
            text.Append("send the same work again; collect it. If it should not finish - a loop that never ends - ");
            text.Append("stop it with cancel_call.");
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
        // Asks for what the tool needs and returns the text of the answer
        public static string Execute(string name, object arguments, RpcDispatcher dispatcher)
        {
            // The two tools that are not about this session are answered before the assertion that the
            // call reached the session it meant
            if (name == "get_guide") return McpAgentGuide.Text;
            if (name == "list_sessions") return ListSessions();
            AssertSession(arguments);
            //
            if (name == "get_status") return Call(dispatcher, "status", null);
            if (name == "describe_api")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                string path = JsonReader.GetString(arguments, "path", null);
                if (path != null) parameters.Add("path", path);
                return Call(dispatcher, "describe", parameters);
            }
            if (name == "read_model")
                return CallAndReadOutput(dispatcher, McpModelScript.GetSource(), "Automation server - read_model");
            if (name == "read_topology")
            {
                List<object> requestedNames = JsonReader.GetArray(arguments, "part_names");
                // An empty list would read every part, which is what leaving it out says
                if (requestedNames != null && requestedNames.Count == 0)
                    throw new McpException("The argument part_names is an empty list. Name at least one part, or " +
                                           "leave it out to read every part. Nothing was done.");
                List<string> names = JsonReader.ToStringList(requestedNames);
                string[] partNames = names == null ? null : names.ToArray();
                int limit = JsonReader.GetInt(arguments, "limit", DefaultTopologyLimit);
                return CallAndReadOutput(dispatcher, McpTopologyScript.GetSource(partNames, limit, TopologyRowBudget),
                                         "Automation server - read_topology");
            }
            if (name == "eval_expression")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("expression", RequireString(arguments, "expression"));
                AddTimeout(arguments, parameters);
                return Call(dispatcher, "eval", parameters);
            }
            if (name == "run_python")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("source", RequireString(arguments, "source"));
                string record = JsonReader.GetString(arguments, "record", null);
                if (record != null) parameters.Add("record", record);
                AddTimeout(arguments, parameters);
                return Call(dispatcher, "exec", parameters);
            }
            if (name == "get_call")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                int callId = JsonReader.GetInt(arguments, "call_id", 0);
                if (callId <= 0) throw new McpException("The argument call_id is required.");
                parameters.Add("call_id", callId);
                AddTimeout(arguments, parameters);
                // A call another tool started is answered the way that tool would have answered it
                string answer = Invoke(dispatcher, "wait", parameters);
                Func<string, string> shape = FindShape(dispatcher, callId);
                if (shape != null && !IsRunning(answer)) return shape(answer);
                if (IsFailed(answer)) throw new McpException(answer);
                return answer;
            }
            if (name == "cancel_call")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                int callId = JsonReader.GetInt(arguments, "call_id", 0);
                if (callId <= 0) throw new McpException("The argument call_id is required.");
                parameters.Add("call_id", callId);
                return Call(dispatcher, "cancel", parameters);
            }
            if (name == "get_history")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("include_journal", JsonReader.GetBool(arguments, "include_journal", false));
                if (JsonReader.HasItem(arguments, "limit"))
                    parameters.Add("limit", JsonReader.GetInt(arguments, "limit", 200));
                return Call(dispatcher, "history", parameters);
            }
            if (name == "run_job")
            {
                string jobName = RequireString(arguments, "name");
                string source = "print(pmx.jobs[" + Quote(jobName) + "].run(wait=False))";
                return Shape(dispatcher, Invoke(dispatcher, "exec", ExecArguments(source)),
                             delegate (string answer) { return GetRunJobAnswer(jobName, answer); });
            }
            if (name == "get_job")
            {
                string jobName = RequireString(arguments, "name");
                bool includeOutput = JsonReader.GetBool(arguments, "include_output", false);
                string job = "pmx.jobs[" + Quote(jobName) + "]";
                string expression = "{'status': " + job + ".status, 'isUpToDate': " + job + ".is_up_to_date" +
                                    (includeOutput ? ", 'output': " + job + ".get_output()" : "") + "}";
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("expression", expression);
                return Call(dispatcher, "eval", parameters);
            }
            if (name == "screenshot")
            {
                // The view is set up through the API, so the picture is taken of what the same calls
                // would have produced from run_python and the endpoint grows no primitive for it
                string setUp = GetViewSource(arguments);
                if (setUp.Length > 0) Call(dispatcher, "exec", ExecArguments(setUp));
                //
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                string fileName = JsonReader.GetString(arguments, "file_name", null);
                if (fileName != null) parameters.Add("file_name", fileName);
                parameters.Add("transparent_background", JsonReader.GetBool(arguments, "transparent_background", false));
                parameters.Add("magnification", JsonReader.GetInt(arguments, "magnification", 1));
                return Call(dispatcher, "screenshot", parameters);
            }
            if (name == "exit_session")
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add("discard_changes", JsonReader.GetBool(arguments, "discard_changes", false));
                return Call(dispatcher, "exit", parameters);
            }
            throw new McpException("There is no tool named " + name + ".");
        }
        // The status the run started with, beside the answer of the call that started it
        private static string GetRunJobAnswer(string jobName, string answer)
        {
            if (IsFailed(answer)) throw new McpException(answer);
            object document;
            string error;
            if (!JsonReader.TryParse(answer, out document, out error)) return answer;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("name", jobName);
            writer.WriteValue("status", JsonReader.GetString(document, "output", "").Trim());
            writer.WriteRawValue("run", answer);
            writer.EndObject();
            return writer.ToString();
        }
        // The snapshot is what the script printed, so it is returned as it was printed rather than
        // wrapped in the answer of the call that ran it. The script is the catalog's own and the same
        // every time, so the Python panel shows a one line note for it instead of the whole source
        private static string CallAndReadOutput(RpcDispatcher dispatcher, string source, string echoNote)
        {
            return Shape(dispatcher, Invoke(dispatcher, "exec", ExecArguments(source), echoNote), ReadOutput);
        }
        private static string ReadOutput(string result)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(result, out document, out error)) return result;
            if (JsonReader.GetBool(document, "failed", false))
                throw new McpException(JsonReader.GetString(document, "error", "The script failed."));
            return JsonReader.GetString(document, "output", "");
        }
        // A tool that shapes the answer of the call it started shapes it whenever it arrives: at once, or - for
        // a call that outlasted its wait and was answered with running - when get_call collects it
        private static string Shape(RpcDispatcher dispatcher, string answer, Func<string, string> shape)
        {
            if (!IsRunning(answer)) return shape(answer);
            //
            object document;
            string error;
            if (JsonReader.TryParse(answer, out document, out error))
            {
                int callId = JsonReader.GetInt(document, "callId", 0);
                if (callId > 0)
                {
                    lock (_shapedCalls)
                    {
                        _shapedCalls.Add(Tuple.Create(dispatcher, callId, shape));
                        if (_shapedCalls.Count > MaximumShapedCalls) _shapedCalls.RemoveAt(0);
                    }
                }
            }
            return answer;
        }
        // Kept after an answer was collected, because one whose response did not reach the client stays
        // collectable; the list is bounded instead
        private static Func<string, string> FindShape(RpcDispatcher dispatcher, int callId)
        {
            lock (_shapedCalls)
            {
                foreach (Tuple<RpcDispatcher, int, Func<string, string>> entry in _shapedCalls)
                {
                    if (entry.Item1 == dispatcher && entry.Item2 == callId) return entry.Item3;
                }
            }
            return null;
        }
        private static bool IsRunning(string result)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(result, out document, out error)) return false;
            return JsonReader.GetBool(document, "running", false);
        }
        // The pmx.view calls the optional screenshot arguments stand for, in the order they have to
        // happen: the tree first, then what is plotted in it, then the camera
        private static string GetViewSource(object arguments)
        {
            StringBuilder source = new StringBuilder();
            string view = JsonReader.GetString(arguments, "view", null);
            if (view != null) source.Append("pmx.view.current = ").Append(Quote(view)).Append("\n");
            //
            string field = JsonReader.GetString(arguments, "field", null);
            string component = JsonReader.GetString(arguments, "component", null);
            // A component alone names no field to plot, and leaving it out would take a picture of a plot
            // the caller did not ask for
            if (field == null && component != null)
                throw new McpException("The argument component of screenshot needs field. Nothing was done.");
            if (field != null)
            {
                source.Append("pmx.view.set_result_field(").Append(Quote(field));
                if (component != null) source.Append(", ").Append(Quote(component));
                source.Append(")\n");
            }
            string orientation = JsonReader.GetString(arguments, "orientation", null);
            if (orientation != null)
                source.Append("pmx.view.set_orientation(").Append(Quote(orientation)).Append(")\n");
            if (JsonReader.GetBool(arguments, "zoom_to_fit", false)) source.Append("pmx.view.zoom_to_fit()\n");
            //
            return source.ToString();
        }
        // The wait is the caller's to choose, within the bound the server checks
        private static void AddTimeout(object arguments, Dictionary<string, object> parameters)
        {
            if (!JsonReader.HasItem(arguments, "timeout")) return;
            //
            int timeout = JsonReader.GetInt(arguments, "timeout", -1);
            if (timeout < 0 || timeout > RpcDispatcher.MaximumWaitSeconds)
                throw new McpException("The argument timeout must be a number of seconds from 0 to " +
                                       RpcDispatcher.MaximumWaitSeconds + ".");
            parameters.Add("timeout", timeout);
        }
        private static Dictionary<string, object> ExecArguments(string source)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("source", source);
            return parameters;
        }
        private static string Call(RpcDispatcher dispatcher, string method, Dictionary<string, object> parameters)
        {
            string result = Invoke(dispatcher, method, parameters);
            // Python that failed is reported as a failed tool call, with the whole answer as the
            // text: the traceback is in it, and so is what was recorded before it failed
            if (IsFailed(result)) throw new McpException(result);
            return result;
        }
        // The one place the endpoint reaches the application, and it reaches it through the dispatcher
        // the named pipe reaches it through. A refusal keeps the name the protocol gave it, so an
        // agent reads the same Busy or ApiError text whichever front it came in by
        private static string Invoke(RpcDispatcher dispatcher, string method,
                                     Dictionary<string, object> parameters)
        {
            return Invoke(dispatcher, method, parameters, null);
        }
        private static string Invoke(RpcDispatcher dispatcher, string method,
                                     Dictionary<string, object> parameters, string echoNote)
        {
            if (dispatcher == null) throw new McpException("The automation server is not running.");
            //
            try
            {
                RpcRequest request = RpcRequest.Create(method, parameters);
                request.EchoNote = echoNote;
                string result = dispatcher.Invoke(request);
                return string.IsNullOrEmpty(result) ? "{}" : result;
            }
            catch (RpcException exception)
            {
                throw new McpException(RpcErrorCode.GetName(exception.Code) + ": " + exception.Message);
            }
            catch (PromptRefusedException exception)
            {
                throw new McpException(RpcErrorCode.GetName(RpcErrorCode.PromptRefused) + ": " +
                                       exception.Message);
            }
            catch (APIs.Python.PythonAPIException exception)
            {
                throw new McpException(RpcErrorCode.GetName(RpcErrorCode.ApiError) + ": " + exception.Message);
            }
        }
        private static bool IsFailed(string result)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(result, out document, out error)) return false;
            return JsonReader.GetBool(document, "failed", false);
        }
        // The directory of what the user has open. The access token of a session is deliberately not
        // in it: an agent never needs it, and a tool result is read and stored in places a token
        // should not reach
        private static string ListSessions()
        {
            List<RpcSessionInfo> sessions = RpcSessionInfoReader.GetSessions();
            int current = Process.GetCurrentProcess().Id;
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("count", sessions.Count);
            writer.BeginArray("sessions");
            foreach (RpcSessionInfo session in sessions)
            {
                writer.BeginObject();
                writer.WriteValue("processId", session.ProcessId);
                writer.WriteValue("isThisSession", session.ProcessId == current);
                writer.WriteValue("url", session.EndpointUrl);
                writer.WriteValue("port", session.Port);
                writer.WriteValue("applicationVersion", session.ApplicationVersion);
                writer.WriteValue("protocolVersion", session.ProtocolVersion);
                writer.WriteValue("modelFileName", session.ModelFileName);
                writer.WriteValue("startTime", session.StartTime);
                writer.EndObject();
            }
            writer.EndArray();
            writer.EndObject();
            return writer.ToString();
        }
        // This endpoint can only ever answer for its own model, so a process id is an assertion rather
        // than a route: a call that names another instance is refused with the address that would have
        // served it, instead of quietly changing a third model
        private static void AssertSession(object arguments)
        {
            int processId = JsonReader.GetInt(arguments, "process_id", 0);
            if (processId <= 0) return;
            //
            int current = Process.GetCurrentProcess().Id;
            if (processId == current) return;
            //
            RpcSessionInfo session = RpcSessionInfoReader.Find(processId);
            string where = session == null || session.EndpointUrl.Length == 0
                           ? "no PrePoMax with that process id is listening"
                           : "it answers on " + session.EndpointUrl;
            throw new McpException("This URL serves PrePoMax process " + current + ", not process " + processId +
                                   " - " + where + ". Call list_sessions for the sessions that are open, and ask " +
                                   "the user to point the client at the URL of the one they mean.");
        }
        private static string RequireString(object arguments, string name)
        {
            string value = JsonReader.GetString(arguments, name, null);
            if (string.IsNullOrEmpty(value)) throw new McpException("The argument " + name + " is required.");
            return value;
        }
        private static string Quote(string value)
        {
            return PythonValueFormatter.Text(value);
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
            schema.Append("\"process_id\": {\"type\": \"integer\", \"minimum\": 1, \"description\": ");
            schema.Append("\"The process id this call expects to reach, from list_sessions. It is checked, not ");
            schema.Append("routed: this URL serves one session, so a call naming another one is refused rather ");
            schema.Append("than served against the wrong model. Leave it out unless you mean to assert.\"}}");
            if (required != null) schema.Append(", \"required\": [").Append(required).Append("]");
            schema.Append(", \"additionalProperties\": false}");
            return schema.ToString();
        }
    }
}
