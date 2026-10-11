// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax.Python
{
    // What one evaluated expression produced. The value is offered three ways because a caller that
    // is not a human needs a machine readable form when there is one, and a readable one when there
    // is not: Json is null for a value that has no JSON form, and the repr is always there.
    public class PythonEvalResult
    {
        // Variables                                                                                                                
        public string Repr { get; set; }
        public string Text { get; set; }
        public string Json { get; set; }
        public string TypeName { get; set; }
        public string Output { get; set; }
        // What the expression printed to sys.stderr, which is output and not a failure
        public string ErrorOutput { get; set; }
        // The error the evaluation ended with, and only that
        public string Error { get; set; }


        // Constructors                                                                                                             
        public PythonEvalResult()
        {
            Repr = "";
            Text = "";
            Json = null;
            TypeName = "";
            Output = "";
            ErrorOutput = "";
            Error = "";
        }


        // Methods                                                                                                                  
        // Everything the user is shown in red: what went to stderr, then the error the evaluation ended with
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
