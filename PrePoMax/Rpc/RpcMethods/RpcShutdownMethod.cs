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

namespace PrePoMax.Rpc
{
    // Stops the server. It closes the door an agent came through and nothing else - the application
    // stays open, because the user is sitting in front of it.
    public static class RpcShutdownMethod
    {
        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            server.RequestStop();
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("stopping", true);
            writer.EndObject();
            return writer.ToString();
        }
    }
}
