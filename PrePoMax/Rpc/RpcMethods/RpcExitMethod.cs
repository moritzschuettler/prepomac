// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using System;
using System.Linq;

namespace PrePoMax.Rpc
{
    // Closes the application, and only for a session that has no window of its own - one started with
    // -g No and the automation server, which nobody is looking at and which otherwise has no way to
    // end but a kill. The session the user works in is closed by the user; an agent may stop the
    // server with shutdown, and that is all.
    public static class RpcExitMethod
    {
        // Variables                                                                                                                
        // The arguments the method takes; any other is refused before anything is done
        public static readonly string[] Parameters = { "discard_changes" };


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            if (server.Form == null || !server.Form.IsWindowHidden)
            {
                throw new RpcException(RpcErrorCode.NotAllowed, "PrePoMax is open on the user's desktop, so " +
                                       "exit_session (exit on the pipe) is refused: the user closes their own " +
                                       "session.");
            }
            // A hidden session answers its own close time questions, and the answer to "save before
            // closing" is no. So an agent that has not saved is told rather than losing the work
            Controller controller = server.Controller;
            bool discard = request.GetBool("discard_changes", false);
            AnalysisJob runningJob = controller == null ? null :
                                     controller.GetAllJobs().FirstOrDefault(job => job.JobStatus == JobStatus.Running ||
                                                                                   job.JobStatus == JobStatus.InQueue);
            if (runningJob != null)
            {
                throw new RpcException(RpcErrorCode.NotAllowed, "The analysis '" + runningJob.Name + "' is running " +
                                       "and closing kills it. Wait for it to finish, or kill it with pmx.jobs['" +
                                       runningJob.Name + "'].kill(), before exit_session (exit on the pipe).");
            }
            if (!discard && controller != null && controller.ModelChanged)
            {
                throw new RpcException(RpcErrorCode.NotAllowed, "The model has unsaved changes and closing " +
                                       "discards them. Save it with pmx.save(file_name) first, or call " +
                                       "exit_session (exit on the pipe) with discard_changes set to true.");
            }
            server.Form.RequestAutomationExit();
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("exiting", true);
            writer.EndObject();
            return writer.ToString();
        }
    }
}
