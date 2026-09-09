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
using System.Diagnostics;
using System.Windows.Forms;

namespace PrePoMax.Rpc
{
    // Liveness, and the versions a client has to agree with before it sends anything else. It is
    // answered without reaching the model, so it is also the call that says whether the application
    // is alive while it is busy.
    public static class RpcPingMethod
    {
        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("protocolVersion", RpcSessionInfoWriter.ProtocolVersion);
            writer.WriteValue("applicationVersion", Application.ProductVersion);
            writer.WriteValue("applicationName", Globals.ProgramName);
            writer.WriteValue("processId", Process.GetCurrentProcess().Id);
            writer.WriteValue("pipeName", server.PipeName == null ? "" : server.PipeName);
            writer.WriteValue("endpointUrl", server.EndpointUrl);
            writer.EndObject();
            return writer.ToString();
        }
    }
}
