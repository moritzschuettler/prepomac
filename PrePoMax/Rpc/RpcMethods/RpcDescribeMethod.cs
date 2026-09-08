// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using PrePoMax.Python;
using System;

namespace PrePoMax.Rpc
{
    // How an agent learns the API instead of being told it. The path is resolved against the live
    // model, so pmx.model.steps['Step-1'].loads describes what is actually there, and a collection
    // reports the names it holds right now.
    public static class RpcDescribeMethod
    {
        // Variables                                                                                                                
        private const string RootPath = "pmx";


        // Static methods                                                                                                           
        public static string Execute(RpcRequest request, RpcServer server)
        {
            string path = request.GetString("path", RootPath);
            if (string.IsNullOrEmpty(path)) path = RootPath;
            //
            server.Form.PrepareAutomationPythonEngine();
            string error;
            object target = PythonEngineManager.EvaluateToApiObject(path, out error);
            if (error != null)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The path " + path + " cannot be resolved: " +
                                                                   error.TrimEnd());
            }
            return RpcDescribeBuilder.Build(path, target);
        }
    }
}
