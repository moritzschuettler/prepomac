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

namespace PrePoMax.Python
{
    // Distinguishes a failure inside a user script from a model failure so that the two can be reported
    // with different process exit codes.
    public class PythonScriptException : CaeException
    {
        // Constructors
        public PythonScriptException(string message)
            : base(message)
        {
        }
        public PythonScriptException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
