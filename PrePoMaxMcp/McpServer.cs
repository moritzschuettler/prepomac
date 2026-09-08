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
using System.Globalization;
using System.IO;
using System.Text;

namespace PrePoMaxMcp
{
    // The MCP end. It speaks JSON-RPC 2.0 on stdio, holds no model state of its own, and translates
    // every tool call into a call on the automation server of a running PrePoMax. The reason it is a
    // process of its own is that an MCP client starts its server, and PrePoMax is already open.
    public class McpServer
    {
        // Variables                                                                                                                
        private const string DefaultProtocolVersion = "2024-11-05";
        private const string ServerName = "prepomax";
        private const string ServerVersion = "1.0.0";
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly int _requestedProcessId;
        private PipeClient _client;
        private int _clientProcessId;


        // Constructor                                                                                                              
        public McpServer(TextReader input, TextWriter output, int requestedProcessId)
        {
            _input = input;
            _output = output;
            _requestedProcessId = requestedProcessId;
            _client = null;
            _clientProcessId = 0;
        }


        // Methods                                                                                                                  
        public void Run()
        {
            while (true)
            {
                string line = _input.ReadLine();
                if (line == null) return;
                if (line.Trim().Length == 0) continue;
                //
                object document;
                string error;
                if (!JsonReader.TryParse(line, out document, out error))
                {
                    Write(Error(null, -32700, "The request is not JSON: " + error));
                    continue;
                }
                string method = JsonReader.GetString(document, "method", null);
                object id = GetId(document);
                bool isNotification = !JsonReader.HasItem(document, "id") ||
                                      JsonReader.GetString(document, "id", null) == null && id == null;
                if (string.IsNullOrEmpty(method))
                {
                    if (!isNotification) Write(Error(id, -32600, "The request names no method."));
                    continue;
                }
                //
                string answer = Handle(method, id, document, isNotification);
                if (answer != null) Write(answer);
            }
        }
        private string Handle(string method, object id, object document, bool isNotification)
        {
            try
            {
                if (method == "initialize") return Initialize(id, document);
                if (method == "ping") return Result(id, "{}");
                if (method == "tools/list") return ToolsList(id);
                if (method == "tools/call") return ToolsCall(id, document);
                // A notification this bridge does not need - initialized, cancelled, progress
                if (method.StartsWith("notifications/", StringComparison.Ordinal)) return null;
                if (isNotification) return null;
                //
                return Error(id, -32601, "This server does not serve " + method + ".");
            }
            catch (Exception exception)
            {
                // A failure of one call must not end the session: the client asks again
                return Error(id, -32603, exception.Message);
            }
        }
        private string Initialize(object id, object document)
        {
            object parameters = JsonReader.GetObject(document, "params");
            string version = JsonReader.GetString(parameters, "protocolVersion", DefaultProtocolVersion);
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("protocolVersion", version);
            writer.BeginObject("capabilities");
            writer.BeginObject("tools");
            writer.EndObject();
            writer.EndObject();
            writer.BeginObject("serverInfo");
            writer.WriteValue("name", ServerName);
            writer.WriteValue("version", ServerVersion);
            writer.EndObject();
            writer.WriteValue("instructions", McpToolCatalog.GetInstructions());
            writer.EndObject();
            return Result(id, writer.ToString());
        }
        private string ToolsList(object id)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.BeginArray("tools");
            foreach (McpTool tool in McpToolCatalog.GetTools())
            {
                writer.BeginObject();
                writer.WriteValue("name", tool.Name);
                writer.WriteValue("title", tool.Title);
                writer.WriteValue("description", tool.Description);
                writer.WriteRawValue("inputSchema", tool.InputSchema);
                writer.BeginObject("annotations");
                writer.WriteValue("readOnlyHint", tool.IsReadOnly);
                writer.EndObject();
                writer.EndObject();
            }
            writer.EndArray();
            writer.EndObject();
            return Result(id, writer.ToString());
        }
        private string ToolsCall(object id, object document)
        {
            object parameters = JsonReader.GetObject(document, "params");
            string name = JsonReader.GetString(parameters, "name", null);
            object arguments = JsonReader.GetObject(parameters, "arguments");
            if (string.IsNullOrEmpty(name)) return Error(id, -32602, "The call names no tool.");
            if (McpToolCatalog.Find(name) == null)
                return Error(id, -32602, "There is no tool named " + name + ".");
            //
            try
            {
                string text = name == "list_sessions" ? ListSessions() : CallSession(name, arguments);
                return ToolResult(id, text, false);
            }
            catch (McpException exception)
            {
                // A tool that cannot be served is a result the agent reads and acts on, not a
                // protocol failure: it may have to wait, start PrePoMax, or ask the user
                return ToolResult(id, exception.Message, true);
            }
            catch (Exception exception)
            {
                return ToolResult(id, "The bridge failed: " + exception.Message, true);
            }
        }
        private string ListSessions()
        {
            List<SessionInfo> sessions = SessionLocator.GetSessions();
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("count", sessions.Count);
            writer.BeginArray("sessions");
            foreach (SessionInfo session in sessions)
            {
                writer.BeginObject();
                writer.WriteValue("processId", session.ProcessId);
                writer.WriteValue("applicationVersion", session.ApplicationVersion);
                writer.WriteValue("protocolVersion", session.ProtocolVersion);
                writer.WriteValue("modelFileName", session.ModelFileName);
                writer.WriteValue("startTime", session.StartTime);
                writer.EndObject();
            }
            writer.EndArray();
            if (sessions.Count == 0) writer.WriteValue("hint", GetNoSessionMessage());
            writer.EndObject();
            return writer.ToString();
        }
        private string CallSession(string name, object arguments)
        {
            int processId = JsonReader.GetInt(arguments, "process_id", 0);
            if (processId <= 0) processId = _requestedProcessId;
            PipeClient client = GetClient(processId);
            return McpToolCatalog.Execute(name, arguments, client);
        }
        private PipeClient GetClient(int processId)
        {
            if (processId <= 0) processId = ResolveOnlySession();
            if (_client != null && _clientProcessId == processId) return _client;
            //
            if (_client != null) _client.Close();
            _clientProcessId = processId;
            _client = new PipeClient(PipeClient.GetPipeName(processId));
            return _client;
        }
        private int ResolveOnlySession()
        {
            List<SessionInfo> sessions = SessionLocator.GetSessions();
            if (sessions.Count == 1) return sessions[0].ProcessId;
            if (sessions.Count == 0) throw new McpException(GetNoSessionMessage());
            //
            StringBuilder message = new StringBuilder();
            message.Append("There are ").Append(sessions.Count).Append(" PrePoMax sessions running, so the call ");
            message.Append("has to name the one it means in process_id:");
            foreach (SessionInfo session in sessions)
            {
                message.Append("\n  ").Append(session.ProcessId).Append(" - ").Append(session.ToString());
            }
            throw new McpException(message.ToString());
        }
        private static string GetNoSessionMessage()
        {
            return "No PrePoMax session is listening. PrePoMax has to be running with its automation server on: " +
                   "the user turns it on in Tools - Automation Server, or leaves it on with " +
                   "Tools - Settings - Automation - Start with the application. Ask the user to do that rather " +
                   "than starting PrePoMax yourself - the point of this server is the session they have open.";
        }
        private string ToolResult(object id, string text, bool isError)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.BeginArray("content");
            writer.BeginObject();
            writer.WriteValue("type", "text");
            writer.WriteValue("text", text == null ? "" : text);
            writer.EndObject();
            writer.EndArray();
            writer.WriteValue("isError", isError);
            writer.EndObject();
            return Result(id, writer.ToString());
        }
        private static object GetId(object document)
        {
            if (!JsonReader.HasItem(document, "id")) return null;
            Dictionary<string, object> items = document as Dictionary<string, object>;
            object id = items["id"];
            if (id is string || id is double) return id;
            return null;
        }
        private static string Result(object id, string resultJson)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("jsonrpc", "2.0");
            WriteId(writer, id);
            writer.WriteRawValue("result", resultJson);
            writer.EndObject();
            return writer.ToString();
        }
        private static string Error(object id, int code, string message)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("jsonrpc", "2.0");
            WriteId(writer, id);
            writer.BeginObject("error");
            writer.WriteValue("code", code);
            writer.WriteValue("message", message == null ? "" : message);
            writer.EndObject();
            writer.EndObject();
            return writer.ToString();
        }
        private static void WriteId(JsonWriter writer, object id)
        {
            if (id is double) writer.WriteValue("id", (double)id);
            else if (id is string) writer.WriteValue("id", (string)id);
            else writer.WriteRawValue("id", "null");
        }
        private void Write(string message)
        {
            // One message per line, and nothing else may ever reach the output: it is the transport
            string[] lines = message.Replace("\r", "").Split('\n');
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < lines.Length; i++) line.Append(lines[i].TrimStart());
            _output.WriteLine(line.ToString());
            _output.Flush();
        }
    }
}
