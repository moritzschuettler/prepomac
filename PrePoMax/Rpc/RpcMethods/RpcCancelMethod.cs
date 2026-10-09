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
    // application it is inside, such as meshing, finishes first, and a blocking wait - time.sleep, a socket,
    // input() - returns first. Each cancel delivers an interrupt of its own, so a call whose code caught the
    // one before is reached by the next. The call then ends the way a failed call ends, and its answer is
    // collected with wait like any other: the commands it recorded before it was stopped stay in the history
    // and are listed there.
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
                                       "started, or its answer was already collected. Call get_status (status on " +
                                       "the pipe) for the calls that are running or waiting to be collected.");
            }
            //
            bool wasRunning = !call.IsFinished;
            bool isDelivered;
            bool isRequested = call.Cancel(out isDelivered);
            bool isFinished = call.Wait(FinishWaitMilliseconds);
            bool hasReachedPython = call.HasReachedPython;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("callId", call.Id);
            writer.WriteValue("method", call.Method);
            writer.WriteValue("cancelRequested", wasRunning);
            writer.WriteValue("isInterrupted", isDelivered);
            writer.WriteValue("isInterruptPending", isRequested && !isDelivered);
            writer.WriteValue("isFinished", isFinished);
            writer.WriteValue("message", GetMessage(call.Id, wasRunning, isRequested, isDelivered, isFinished,
                                                    hasReachedPython));
            writer.EndObject();
            return writer.ToString();
        }
        private static string GetMessage(int callId, bool wasRunning, bool isRequested, bool isDelivered,
                                         bool isFinished, bool hasReachedPython)
        {
            string call = "Call " + callId.ToString(CultureInfo.InvariantCulture);
            string collect = "Collect its answer with get_call - the wait method on the pipe.";
            if (!wasRunning) return call + " had already finished, so there was nothing to cancel. " + collect;
            //
            string recorded = " The commands it recorded before it stopped stay in the command history; its answer " +
                              "lists them, and the user can remove them in Edit - Edit History.";
            string notInterruptible = "An operation of PrePoMax it is inside - meshing, an import, a regeneration - " +
                                      "cannot be interrupted and finishes first, and a blocking wait - time.sleep, a " +
                                      "socket, input() - returns first.";
            if (isDelivered && isFinished)
            {
                return call + " was interrupted and has stopped. " + collect + " It ends with a KeyboardInterrupt " +
                       "error." + recorded;
            }
            if (isDelivered)
            {
                return call + " was interrupted but has not stopped yet. The interrupt is raised at the next Python " +
                       "statement the call runs. " + notInterruptible + " Code that catches the interrupt - a bare " +
                       "except - keeps running: cancel it again, and every cancel delivers a new interrupt. " + collect +
                       recorded;
            }
            if (isRequested && isFinished && !hasReachedPython)
            {
                return call + " was cancelled before its Python started, so nothing was run. " + collect;
            }
            if (isRequested && isFinished)
            {
                return call + " finished before the interrupt reached its Python. " + collect;
            }
            if (isRequested && !hasReachedPython)
            {
                return call + " has not reached its Python yet - it is waiting for the application to be idle - so " +
                       "the interrupt is pending: its Python does not run when it gets there, and the call ends with " +
                       "a KeyboardInterrupt error. " + collect;
            }
            if (isRequested)
            {
                return call + " is inside an operation of PrePoMax that holds Python - meshing, an import, a " +
                       "regeneration - so the interrupt has not been delivered yet; it is raised when that operation " +
                       "returns to Python. " + collect + recorded;
            }
            if (isFinished)
            {
                return call + " was not running Python when the cancel arrived and has finished: it had already " +
                       "run its Python, so it completed as it would have, or it ended before starting it. " + collect;
            }
            return call + " had already run its Python when the cancel arrived and is finishing after it, so it " +
                   "completes as it would have. " + collect;
        }
    }
}
