// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeGlobals
{
    public class PromptRefusedException : CaeException
    {
        // Constructors
        public PromptRefusedException(string message)
            : base(message)
        {
        }
        public PromptRefusedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
