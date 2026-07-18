// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace UserControls
{
    public class PythonTextBoxCommandEventArgs : EventArgs
    {
        // Properties                                                                                                               
        public string Command { get; private set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public bool NeedMoreInput { get; set; }


        // Constructors                                                                                                             
        public PythonTextBoxCommandEventArgs(string command)
        {
            Command = command;
            Output = "";
            Error = "";
            NeedMoreInput = false;
        }
    }
}
