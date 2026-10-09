// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System;
using System.Globalization;

namespace PrePoMax.Rpc
{
    // What the server is, and what it will and will not do right now. It runs on the connection thread
    // and asks the user interface thread only for the busy reason, and it is never refused for being
    // busy: being busy, and saying why, is the answer - a window that does not answer that question is
    // reported as busy rather than as a failure. It names the methods it serves, so a client discovers
    // the protocol instead of being told it out of band.
    public static class RpcStatusMethod
    {
        // Variables                                                                                                                
        // The arguments the method takes; any other is refused before anything is done
        public static readonly string[] Parameters = { };
        private static readonly string[] Methods = { "ping", "status", "describe", "eval", "exec", "wait", "cancel", "history",
                                                     "screenshot", "shutdown", "exit" };


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            AutomationSettings settings = server.Settings;
            // An agent call that is still running is the reason, and saying so does not need the window,
            // which a long call may be keeping busy. Otherwise the reason lives on the thread that owns
            // the window; this runs on a connection thread
            RpcRunningCall runningCall = server.GetDispatcher().GetRunningCall();
            string busyReason;
            if (runningCall != null) busyReason = runningCall.GetBusyReason();
            else busyReason = GetWindowBusyReason(server);
            if (busyReason != null && busyReason.Length == 0) busyReason = null;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("protocolVersion", RpcSessionInfoWriter.ProtocolVersion);
            writer.WriteValue("isRunning", server.IsRunning);
            writer.WriteValue("pipeName", server.PipeName == null ? "" : server.PipeName);
            writer.WriteValue("endpointUrl", server.EndpointUrl);
            writer.WriteValue("endpointPort", server.EndpointPort);
            writer.WriteValue("startTime", server.StartTime.ToString("s", CultureInfo.InvariantCulture));
            writer.WriteValue("connectionCount", server.ConnectionCount);
            writer.WriteValue("pipeConnectionCount", server.PipeConnectionCount);
            writer.WriteValue("isMcpClientActive", server.HttpServer.IsClientActive);
            writer.WriteValue("isWindowHidden", server.Form != null && server.Form.IsWindowHidden);
            writer.WriteValue("lastError", server.LastError == null ? "" : server.LastError);
            writer.WriteValue("isReadOnly", server.IsReadOnly);
            writer.WriteValue("echoToPythonPanel", settings.EchoToPythonPanel);
            writer.WriteValue("requestTimeout", server.RequestTimeout);
            writer.WriteValue("maximumCallTimeout", RpcDispatcher.MaximumWaitSeconds);
            WriteCalls(writer, server);
            writer.WriteValue("isIdle", busyReason == null);
            // A recorded script suppresses command recording while it runs, so an agent can see
            // whether what it does is reaching the history the user can edit and regenerate
            writer.WriteValue("isCommandRecordingSuppressed", server.Controller != null &&
                                                              server.Controller.IsCommandRecordingSuppressed);
            writer.WriteValue("busyReason", busyReason == null ? "" : busyReason);
            WriteModel(writer, server);
            writer.BeginArray("methods");
            for (int i = 0; i < Methods.Length; i++) writer.WriteValue(Methods[i]);
            writer.EndArray();
            writer.EndObject();
            return writer.ToString();
        }
        private static string GetWindowBusyReason(RpcServer server)
        {
            if (server.Form == null) return null;
            //
            try
            {
                return server.GetUiThread().GetBusyReason(server.RequestTimeout);
            }
            catch (RpcException exception)
            {
                if (exception.Code != RpcErrorCode.Busy) throw;
                return "The window is not responding: PrePoMax is inside something that does not release the " +
                       "user interface, such as a mesher or a dialog waiting for the user.";
            }
        }
        // The exec and eval calls that outlasted the wait of their caller: the one still running, and
        // those that finished and whose answer wait has not collected yet
        private static void WriteCalls(JsonWriter writer, RpcServer server)
        {
            writer.BeginArray("calls");
            foreach (RpcRunningCall call in server.GetDispatcher().GetCalls())
            {
                writer.BeginObject();
                writer.WriteValue("callId", call.Id);
                writer.WriteValue("method", call.Method);
                writer.WriteValue("isFinished", call.IsFinished);
                writer.WriteValue("elapsed", call.Elapsed);
                writer.EndObject();
            }
            writer.EndArray();
        }
        private static void WriteModel(JsonWriter writer, RpcServer server)
        {
            Controller controller = server.Controller;
            writer.BeginObject("model");
            if (controller == null)
            {
                writer.WriteValue("isOpen", false);
                writer.EndObject();
                return;
            }
            FeModel model = controller.Model;
            writer.WriteValue("isOpen", model != null);
            writer.WriteValue("name", model == null || model.Name == null ? "" : model.Name);
            string fileName = server.ModelFileName;
            writer.WriteValue("fileName", fileName == null ? "" : fileName);
            writer.WriteValue("hasUnsavedChanges", controller.ModelChanged);
            writer.WriteValue("currentView", controller.CurrentView.ToString());
            if (model != null && model.Properties != null)
                writer.WriteValue("modelSpace", model.Properties.ModelSpace.ToString());
            if (model != null && model.UnitSystem != null)
                writer.WriteValue("unitSystem", model.UnitSystem.UnitSystemType.ToString());
            writer.WriteValue("commandCount", controller.GetCommands() == null ? 0 : controller.GetCommands().Count);
            writer.EndObject();
        }
    }
}
