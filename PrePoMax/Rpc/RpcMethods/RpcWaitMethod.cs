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
    // Collects the answer of an exec or an eval that outlasted the wait of its caller. It waits for the
    // call as long as it is asked to - the request timeout when it is not told - and answers with what
    // the call answered, or once more that it is still running. It is never refused for being busy and
    // never waits for the turn, because the call it waits for is the one holding it.
    public static class RpcWaitMethod
    {
        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            int callId = request.GetInt("call_id", 0);
            if (callId <= 0)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument call_id is required: the callId " +
                                                                  "a still running call was answered with.");
            }
            int waitSeconds = RpcDispatcher.GetWaitSeconds(request, "timeout", server.RequestTimeout);
            return server.GetDispatcher().Collect(callId, waitSeconds);
        }
    }
}
