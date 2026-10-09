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
    // A session without a window that was started for the automation server, and could not start it -
    // there is no access token, or no free port. Nobody could ever reach such a session, so it ends the
    // run with ExitCodeEnum.OptionError rather than being left running with nothing to serve.
    public class RpcStartException : CaeException
    {
        // Constructor                                                                                                              
        public RpcStartException(string message)
            : base(message)
        {
        }
    }
}
