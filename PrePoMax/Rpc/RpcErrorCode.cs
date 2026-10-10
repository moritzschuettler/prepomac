// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax.Rpc
{
    // The JSON-RPC 2.0 codes the automation server answers with. The standard range is defined by the
    // specification; the application range says why a call the protocol accepted could not be served,
    // and the MCP endpoint names each one in the text of a failed tool, so an agent can act on it.
    public static class RpcErrorCode
    {
        // Variables                                                                                                                
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
        // The application range
        public const int Busy = -32001;
        public const int PromptRefused = -32003;
        public const int ApiError = -32004;
        public const int NotAllowed = -32005;


        // Static methods                                                                                                           
        public static string GetName(int code)
        {
            if (code == ParseError) return "ParseError";
            else if (code == InvalidRequest) return "InvalidRequest";
            else if (code == MethodNotFound) return "MethodNotFound";
            else if (code == InvalidParams) return "InvalidParams";
            else if (code == InternalError) return "InternalError";
            else if (code == Busy) return "Busy";
            else if (code == PromptRefused) return "PromptRefused";
            else if (code == ApiError) return "ApiError";
            else if (code == NotAllowed) return "NotAllowed";
            return "Error";
        }
    }
}
