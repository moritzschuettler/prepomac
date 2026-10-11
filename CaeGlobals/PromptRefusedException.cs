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
        // Variables
        // The message opens with it, so a refusal can be recognized after it went through the text of a traceback
        public const string RefusalText = "An interactive prompt was refused in the strict non-interactive mode.";


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
