// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;

namespace PrePoMax.Rpc
{
    // A failure a handler wants reported with a particular JSON-RPC code. Anything else that reaches
    // the dispatcher is an internal error, because it was not foreseen.
    public class RpcException : CaeException
    {
        // Variables                                                                                                                
        private readonly int _code;


        // Properties                                                                                                               
        public int Code { get { return _code; } }


        // Constructors                                                                                                             
        public RpcException(int code, string message)
            : base(message)
        {
            _code = code;
        }
        public RpcException(int code, string message, Exception inner)
            : base(message, inner)
        {
            _code = code;
        }
    }
}
