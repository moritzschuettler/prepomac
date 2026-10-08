// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System.Globalization;

namespace PrePoMax.Rpc
{
    // Stops an exec or an eval that is still running, which is the only way out of a call that would never end
    // by itself: a loop that never exits holds the turn, and every later call that reaches the model waits for
    // it. Like wait it is never refused for being busy and never waits for the turn, because the call it stops
    // is the one holding it. It changes nothing in the model itself, so read only mode does not refuse it.
    //
    // The call is interrupted where Python can be interrupted - at its next statement - so an operation of the
    // application it is inside, such as meshing, finishes first. The call then ends the way a failed call ends,
    // and its answer is collected with wait like any other: the commands it recorded before it was stopped stay
    // in the history and are listed there.
    public static class RpcCancelMethod
    {
        // Variables                                                                                                                
        private const int FinishWaitMilliseconds = 2000;


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            int callId = request.GetInt("call_id", 0);
            if (callId <= 0)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The argument call_id is required: the callId " +
                                                                  "a still running call was answered with.");
            }
            RpcRunningCall call = server.GetDispatcher().FindCall(callId);
            if (call == null)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "There is no automation call " +
                                       callId.ToString(CultureInfo.InvariantCulture) + " to cancel: it was never " +
                                       "started, or its answer was already collected. Call status for the calls " +
                                       "that are running or waiting to be collected.");
            }
            //
            bool wasRunning = !call.IsFinished;
            bool isInterrupted = call.Cancel();
            bool isFinished = call.Wait(FinishWaitMilliseconds);
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("callId", call.Id);
            writer.WriteValue("method", call.Method);
            writer.WriteValue("cancelRequested", wasRunning);
            writer.WriteValue("isInterrupted", isInterrupted);
            writer.WriteValue("isFinished", isFinished);
            writer.WriteValue("message", GetMessage(call.Id, wasRunning, isInterrupted, isFinished));
            writer.EndObject();
            return writer.ToString();
        }
        private static string GetMessage(int callId, bool wasRunning, bool isInterrupted, bool isFinished)
        {
            string call = "Call " + callId.ToString(CultureInfo.InvariantCulture);
            string collect = "Collect its answer with get_call - the wait method on the pipe.";
            if (!wasRunning) return call + " had already finished, so there was nothing to cancel. " + collect;
            //
            string recorded = " The commands it recorded before it stopped stay in the command history; its answer " +
                              "lists them, and the user can remove them in Edit - Edit History.";
            if (isInterrupted && isFinished)
            {
                return call + " was interrupted and has stopped. " + collect + " It ends with a KeyboardInterrupt " +
                       "error." + recorded;
            }
            if (isInterrupted)
            {
                return call + " was interrupted but has not stopped yet. An interrupt lands at the next Python " +
                       "statement the call runs; an operation of PrePoMax it is inside - meshing, an import, a " +
                       "regeneration - cannot be interrupted and finishes first. " + collect + " It ends with a " +
                       "KeyboardInterrupt error." + recorded;
            }
            if (isFinished)
            {
                return call + " was not running Python when the cancel arrived: a call that had not reached its " +
                       "Python did not start it, and one that was past it finished as it would have. " + collect;
            }
            return call + " was not running Python when the cancel arrived and is still working: it is waiting " +
                   "for the application to be idle, and will not start its Python, or it is finishing after its " +
                   "Python. " + collect;
        }
    }
}
