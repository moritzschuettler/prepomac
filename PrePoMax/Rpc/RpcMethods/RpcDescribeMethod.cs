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
using System.Collections.Generic;

namespace PrePoMax.Rpc
{
    // How an agent learns the API instead of being told it. The path is resolved against the live
    // model, so pmx.model.steps['Step-1'].loads describes what is actually there, and a collection
    // reports the names it holds right now.
    //
    // A name that is not a path is taken as an API type name, so the half of the API that hangs off
    // something the model does not have yet can still be read. A type name followed by a member path
    // - Result.mesh - is walked through the declared member types for the same reason. The path is
    // tried first: a mistyped path is reported as a path, not as a missing type.
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
            if (error == null) return RpcDescribeBuilder.Build(path, target);
            //
            List<Type> types = RpcApiTypeLocator.Find(path);
            if (types.Count == 1) return RpcDescribeBuilder.BuildForType(RpcApiTypeLocator.GetShortName(types[0]),
                                                                        types[0]);
            if (types.Count > 1)
            {
                throw new RpcException(RpcErrorCode.InvalidParams, "The name " + path + " is shared by " +
                                                                   types.Count + " API types. Ask for one of " +
                                                                   RpcApiTypeLocator.GetNames(types) + ".");
            }
            //
            string resolvedName;
            string memberError;
            Type memberType = RpcApiTypeLocator.FindByMemberPath(path, out resolvedName, out memberError);
            if (memberType != null) return RpcDescribeBuilder.BuildForType(resolvedName, memberType);
            //
            throw new RpcException(RpcErrorCode.InvalidParams, "The path " + path + " cannot be resolved: " +
                                                               error.TrimEnd() + " It is not an API type name " +
                                                               "either." +
                                                               (memberError == null ? "" : " " + memberError));
        }
    }
}
