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

namespace APIs.Python
{
    // The model keys its collections without regard to case and trims every name it is given, so a name the API
    // checks is compared the same way. A check that let 'steel' past 'Steel' only moved the refusal into the model,
    // after the item had been renamed in place.
    internal static class PythonNameComparer
    {
        // Methods
        public static bool AreSame(string name1, string name2)
        {
            if (name1 == null || name2 == null) return name1 == name2;
            //
            return string.Equals(name1.Trim(), name2.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        public static bool Contains(IEnumerable<string> names, string name)
        {
            foreach (string existingName in names)
            {
                if (AreSame(existingName, name)) return true;
            }
            return false;
        }
    }
}
