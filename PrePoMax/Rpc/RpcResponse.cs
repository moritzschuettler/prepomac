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
using System.Text;

namespace PrePoMax.Rpc
{
    // One response line. A handler renders its result as a JSON value with a JsonWriter of its own and
    // this composes the envelope around it, so a handler that fails half way through writing cannot
    // produce a half written response.
    public class RpcResponse
    {
        // Variables                                                                                                                
        private readonly string _line;


        // Properties                                                                                                               
        public string Line { get { return _line; } }


        // Constructor                                                                                                              
        private RpcResponse(string line)
        {
            _line = line;
        }


        // Static methods                                                                                                           
        public static RpcResponse FromResult(object id, string resultJson)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("jsonrpc", RpcRequest.Version);
            WriteId(writer, id);
            writer.WriteRawValue("result", string.IsNullOrEmpty(resultJson) ? "{}" : resultJson);
            writer.EndObject();
            return new RpcResponse(Flatten(writer.ToString()));
        }
        public static RpcResponse FromError(object id, int code, string message)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("jsonrpc", RpcRequest.Version);
            WriteId(writer, id);
            writer.BeginObject("error");
            writer.WriteValue("code", code);
            writer.WriteValue("name", RpcErrorCode.GetName(code));
            writer.WriteValue("message", message == null ? "" : message);
            writer.EndObject();
            writer.EndObject();
            return new RpcResponse(Flatten(writer.ToString()));
        }
        private static void WriteId(JsonWriter writer, object id)
        {
            if (id is double) writer.WriteValue("id", (double)id);
            else if (id is string) writer.WriteValue("id", (string)id);
            else writer.WriteRawValue("id", "null");
        }
        // The transport is one response per line, and JsonWriter indents for a human reader
        private static string Flatten(string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < lines.Length; i++) line.Append(lines[i].TrimStart());
            return line.ToString();
        }
    }
}
