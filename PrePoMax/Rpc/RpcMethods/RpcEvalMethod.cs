// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Commands;
using PrePoMax.Python;
using System;
using System.Collections.Generic;

namespace PrePoMax.Rpc
{
    // Evaluates one expression against the live model and hands the value back. It is the reading
    // half of the surface: nothing it evaluates is meant to change the model, so it reports the
    // commands it recorded rather than trusting that there were none - an expression that calls an
    // add method would show up there, and a client can see that it asked for the wrong thing.
    public static class RpcEvalMethod
    {
        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            string expression = request.RequireString("expression");
            Controller controller = server.Controller;
            if (controller == null) throw new RpcException(RpcErrorCode.InternalError, "There is no open model.");
            //
            server.Form.PrepareAutomationPythonEngine();
            int commandsBefore = RpcExecMethod.GetCommandCount(controller);
            DateTime started = DateTime.Now;
            PythonEvalResult result;
            if (RpcExecMethod.IsCancelledBeforeStart())
            {
                result = new PythonEvalResult();
                result.Error = RpcExecMethod.CancelledBeforeStartError;
            }
            else result = PythonEngineManager.Evaluate(expression);
            double elapsed = (DateTime.Now - started).TotalSeconds;
            List<Command> recorded = RpcExecMethod.GetRecordedCommands(controller, commandsBefore);
            //
            server.Form.EchoAutomationExecution("Automation server - eval", expression, result.Output, result.Error);
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("expression", expression);
            writer.WriteValue("repr", result.Repr);
            writer.WriteValue("text", result.Text);
            writer.WriteValue("type", result.TypeName);
            writer.WriteRawValue("value", result.Json);
            writer.WriteValue("hasValue", result.Json != null);
            writer.WriteValue("output", result.Output);
            writer.WriteValue("error", result.Error);
            writer.WriteValue("failed", !string.IsNullOrEmpty(result.Error));
            writer.WriteValue("elapsed", elapsed);
            RpcExecMethod.WriteRecordedCommands(writer, recorded);
            if (recorded.Count > 0)
            {
                writer.WriteValue("warning", "The expression recorded a command. eval_expression (eval on the " +
                                             "pipe) is meant for reading; use run_python (exec on the pipe) for " +
                                             "anything that changes the model.");
            }
            RpcExecMethod.WriteCancellation(writer, !string.IsNullOrEmpty(result.Error));
            writer.EndObject();
            return writer.ToString();
        }
    }
}
