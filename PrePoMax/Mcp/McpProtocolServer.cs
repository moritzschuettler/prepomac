// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Rpc;
using System;
using System.Collections.Generic;
using System.IO;

namespace PrePoMax.Mcp
{
    // The MCP protocol, as one request in and one response out. It is a tools only server, so it
    // serves four methods and no more, and it holds no state of its own: no session id is issued. The
    // protocol version is negotiated the way the specification says: the client's own version when this
    // server supports it, and the latest one it supports otherwise, which the client may then refuse.
    //
    // It is deliberately unaware of how the request arrived. The transport is McpHttpServer, and the
    // only thing this needs from the application is the dispatcher every tool call goes through.
    public static class McpProtocolServer
    {
        // Variables                                                                                                                
        // Oldest first, so the last one is the latest this server supports. Every one of them is served
        // by the same tools only surface; a batch is refused under all of them
        private static readonly string[] SupportedProtocolVersions = { "2024-11-05", "2025-03-26", "2025-06-18" };
        // A picture larger than this is left on disk and named by its path only
        private const long MaximumImageLength = 4 * 1024 * 1024;
        private const string ServerName = "prepomax";
        private const string ServerVersion = "1.0.0";


        // Static methods                                                                                                           
        // Returns the response to send, or null when the request was a notification and there is
        // nothing to answer. A request that is not JSON is answered rather than dropped, because the
        // caller reached the protocol and a parse error is the protocol's own answer.
        //
        // A notification is never answered, so nothing it asks for is done either: a tools/call sent
        // without an id would change the model with no way to tell the caller what it did, and MCP
        // allows a tools/call only as a request
        public static string Handle(string requestBody, RpcDispatcher dispatcher)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(requestBody, out document, out error))
                return Error(null, -32700, "The request is not JSON: " + error);
            if (document is List<object>)
                return Error(null, -32600, "A batch of requests is not supported. Send one JSON-RPC object per request.");
            if (!(document is Dictionary<string, object>))
                return Error(null, -32600, "The request must be a JSON-RPC object.");
            //
            string method = JsonReader.GetString(document, "method", null);
            object id = GetId(document);
            bool isNotification = !JsonReader.HasItem(document, "id");
            // MCP narrows JSON-RPC here: the id of a request is a string or a number, and never null
            if (!isNotification && id == null)
                return Error(null, -32600, "The id of a request must be a string or a number.");
            // A notification this server does not need - initialized, cancelled, progress - and a
            // request sent as a notification, which is not run because it could not be answered
            if (isNotification) return null;
            if (string.IsNullOrEmpty(method)) return Error(id, -32600, "The request names no method.");
            try
            {
                if (method == "initialize") return Initialize(id, document);
                if (method == "ping") return Result(id, "{}");
                if (method == "tools/list") return ToolsList(id);
                if (method == "tools/call") return ToolsCall(id, document, dispatcher);
                //
                return Error(id, -32601, "This server does not serve " + method + ".");
            }
            catch (Exception exception)
            {
                // A failure of one call must not end the session: the client asks again
                return Error(id, -32603, exception.Message);
            }
        }
        private static string Initialize(object id, object document)
        {
            object parameters = JsonReader.GetObject(document, "params");
            string version = NegotiateProtocolVersion(JsonReader.GetString(parameters, "protocolVersion", null));
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
        private static string NegotiateProtocolVersion(string requested)
        {
            foreach (string supported in SupportedProtocolVersions)
            {
                if (supported == requested) return supported;
            }
            return SupportedProtocolVersions[SupportedProtocolVersions.Length - 1];
        }
        private static string ToolsList(object id)
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
        private static string ToolsCall(object id, object document, RpcDispatcher dispatcher)
        {
            object parameters = JsonReader.GetObject(document, "params");
            string name = JsonReader.GetString(parameters, "name", null);
            object arguments = JsonReader.GetObject(parameters, "arguments");
            if (string.IsNullOrEmpty(name)) return Error(id, -32602, "The call names no tool.");
            McpTool tool = McpToolCatalog.Find(name);
            if (tool == null)
                return Error(id, -32602, "There is no tool named " + name + ". The tools are " + GetToolNames() + ".");
            if (JsonReader.HasItem(parameters, "arguments") && arguments == null)
                return ToolResult(id, "The arguments of " + name + " must be a JSON object.", true);
            // An argument the tool does not take would otherwise be dropped without a word, and the call
            // would run without what the caller meant to ask for
            string unknown = GetUnknownArguments(tool, arguments);
            if (unknown != null) return ToolResult(id, unknown, true);
            // A value of the wrong type or out of range would otherwise be read as the default, and the
            // call would run as if the argument had not been given
            string invalid = McpArgumentValidator.Validate(tool, arguments);
            if (invalid != null) return ToolResult(id, invalid, true);
            //
            try
            {
                string text = McpToolCatalog.Execute(name, arguments, dispatcher);
                if (name == "screenshot") return ToolResult(id, text, false, ReadImage(text));
                return ToolResult(id, text, false);
            }
            catch (McpException exception)
            {
                // A tool that cannot be served is a result the agent reads and acts on, not a
                // protocol failure: it may have to wait for the user, or ask them something
                return ToolResult(id, exception.Message, true);
            }
            catch (Exception exception)
            {
                return ToolResult(id, "The endpoint failed to serve the tool: " + exception.Message, true);
            }
        }
        private static string GetToolNames()
        {
            List<string> names = new List<string>();
            foreach (McpTool tool in McpToolCatalog.GetTools()) names.Add(tool.Name);
            return string.Join(", ", names.ToArray());
        }
        // The names the schema of the tool declares are the arguments it takes, so the check cannot
        // drift from what tools/list tells the client
        private static string GetUnknownArguments(McpTool tool, object arguments)
        {
            Dictionary<string, object> items = arguments as Dictionary<string, object>;
            if (items == null || items.Count == 0) return null;
            //
            object schema;
            string error;
            if (!JsonReader.TryParse(tool.InputSchema, out schema, out error)) return null;
            Dictionary<string, object> properties = JsonReader.GetObject(schema, "properties");
            if (properties == null) properties = new Dictionary<string, object>();
            //
            List<string> unknown = new List<string>();
            foreach (string key in items.Keys)
            {
                if (!properties.ContainsKey(key)) unknown.Add(key);
            }
            if (unknown.Count == 0) return null;
            //
            string taken = "It takes no arguments.";
            if (properties.Count > 0) taken = "It takes " + string.Join(", ", new List<string>(properties.Keys).ToArray()) + ".";
            return "The tool " + tool.Name + " has no argument named " + string.Join(", ", unknown.ToArray()) +
                   ", so nothing was done. " + taken;
        }
        // The picture a screenshot wrote, as base64, or null when there is none or it is too large to carry
        private static string ReadImage(string answer)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(answer, out document, out error)) return null;
            string fileName = JsonReader.GetString(document, "fileName", null);
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName)) return null;
            //
            FileInfo file = new FileInfo(fileName);
            if (file.Length == 0 || file.Length > MaximumImageLength) return null;
            return Convert.ToBase64String(File.ReadAllBytes(fileName));
        }
        private static string ToolResult(object id, string text, bool isError)
        {
            return ToolResult(id, text, isError, null);
        }
        private static string ToolResult(object id, string text, bool isError, string pngBase64)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.BeginArray("content");
            writer.BeginObject();
            writer.WriteValue("type", "text");
            writer.WriteValue("text", text == null ? "" : text);
            writer.EndObject();
            if (pngBase64 != null)
            {
                writer.BeginObject();
                writer.WriteValue("type", "image");
                writer.WriteValue("data", pngBase64);
                writer.WriteValue("mimeType", "image/png");
                writer.EndObject();
            }
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
    }
}
