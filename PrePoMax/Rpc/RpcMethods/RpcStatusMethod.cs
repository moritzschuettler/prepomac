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
    // What the server is, and what it will and will not do right now. It runs on the user interface
    // thread but is never refused for being busy: being busy, and saying why, is the answer. It names
    // the methods it serves, so a client discovers the protocol instead of being told it out of band.
    public static class RpcStatusMethod
    {
        // Variables                                                                                                                
        private static readonly string[] Methods = { "ping", "status", "describe", "eval", "exec", "history",
                                                     "screenshot", "shutdown", "exit" };


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            AutomationSettings settings = server.Settings;
            // The reason lives on the thread that owns the window; this runs on a connection thread
            string busyReason = server.Form == null ? null : server.GetUiThread().GetBusyReason(settings.RequestTimeout);
            if (busyReason != null && busyReason.Length == 0) busyReason = null;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("protocolVersion", RpcSessionInfoWriter.ProtocolVersion);
            writer.WriteValue("isRunning", server.IsRunning);
            writer.WriteValue("pipeName", server.PipeName == null ? "" : server.PipeName);
            writer.WriteValue("startTime", server.StartTime.ToString("s", CultureInfo.InvariantCulture));
            writer.WriteValue("connectionCount", server.ConnectionCount);
            writer.WriteValue("isWindowHidden", server.Form != null && server.Form.IsWindowHidden);
            writer.WriteValue("lastError", server.LastError == null ? "" : server.LastError);
            writer.WriteValue("isReadOnly", server.IsReadOnly);
            writer.WriteValue("echoToPythonPanel", settings.EchoToPythonPanel);
            writer.WriteValue("requestTimeout", settings.RequestTimeout);
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
            writer.WriteValue("fileName", controller.OpenedFileName == null ? "" : controller.OpenedFileName);
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
