// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;

namespace UserControls
{
    public class PythonTextBoxAutocompleteEventArgs : EventArgs
    {
        public string Expression { get; private set; }
        public string Prefix { get; private set; }
        public List<string> Completions { get; private set; }

        public PythonTextBoxAutocompleteEventArgs(string expression, string prefix)
        {
            Expression = expression;
            Prefix = prefix;
            Completions = new List<string>();
        }
    }
}
