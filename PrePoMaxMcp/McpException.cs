// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMaxMcp
{
    // Something the agent has to be told about rather than a failure of the bridge itself: no session
    // to attach to, a server that is off, a connection that dropped. Every message here is written to
    // be read by an agent that has to decide what to do next.
    public class McpException : Exception
    {
        // Constructors                                                                                                             
        public McpException(string message)
            : base(message)
        {
        }
    }
}
