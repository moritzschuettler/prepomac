// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Python
{
    public class PythonEngineResult
    {
        // Variables
        public string Output { get; set; }
        // What the script printed to sys.stderr - warnings, logging - which is output, not a failure. A console line
        // is the exception: the console reports the error of a line there itself
        public string ErrorOutput { get; set; }
        // The error the script ended with, and only that, so a script failed exactly when this is not empty
        public string Error { get; set; }
        public bool NeedMoreInput { get; set; }


        // Constructors
        public PythonEngineResult()
        {
            Output = "";
            ErrorOutput = "";
            Error = "";
            NeedMoreInput = false;
        }


        // Methods
        // Everything the user is shown in red: what went to stderr, then the error the script ended with
        public string GetErrorText()
        {
            string errorOutput = ErrorOutput ?? "";
            string error = Error ?? "";
            if (errorOutput.Length > 0 && error.Length > 0 && !errorOutput.EndsWith("\n"))
                errorOutput += Environment.NewLine;
            return errorOutput + error;
        }
    }
}
