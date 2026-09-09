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

namespace PrePoMax.Mcp
{
    // The MCP protocol, as one request in and one response out. It is a tools only server, so it
    // serves four methods and no more, and it holds no state of its own: no session id is issued, and
    // the client's own protocol version is echoed back, so a client written against either revision of
    // the specification can talk to it.
    //
    // It is deliberately unaware of how the request arrived. The transport is McpHttpServer, and the
    // only thing this needs from the application is the dispatcher every tool call goes through.
    public static class McpProtocolServer
    {
        // Variables                                                                                                                
        private const string DefaultProtocolVersion = "2024-11-05";
        private const string ServerName = "prepomax";
        private const string ServerVersion = "1.0.0";


        // Static methods                                                                                                           
        // Returns the response to send, or null when the request was a notification and there is
        // nothing to answer. A request that is not JSON is answered rather than dropped, because the
        // caller reached the protocol and a parse error is the protocol's own answer
        public static string Handle(string requestBody, RpcDispatcher dispatcher)
        {
            object document;
            string error;
            if (!JsonReader.TryParse(requestBody, out document, out error))
                return Error(null, -32700, "The request is not JSON: " + error);
            //
            string method = JsonReader.GetString(document, "method", null);
            object id = GetId(document);
            bool isNotification = !JsonReader.HasItem(document, "id") || id == null;
            if (string.IsNullOrEmpty(method))
            {
                if (isNotification) return null;
                return Error(id, -32600, "The request names no method.");
            }
            try
            {
                if (method == "initialize") return Initialize(id, document);
                if (method == "ping") return Result(id, "{}");
                if (method == "tools/list") return ToolsList(id);
                if (method == "tools/call") return ToolsCall(id, document, dispatcher);
                // A notification this server does not need - initialized, cancelled, progress
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
        private static string Initialize(object id, object document)
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
            if (McpToolCatalog.Find(name) == null)
                return Error(id, -32602, "There is no tool named " + name + ".");
            //
            try
            {
                return ToolResult(id, McpToolCatalog.Execute(name, arguments, dispatcher), false);
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
        private static string ToolResult(object id, string text, bool isError)
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
    }
}
