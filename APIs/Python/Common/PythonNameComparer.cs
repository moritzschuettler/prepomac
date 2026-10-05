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
        // The name setter of a model item throws on a name it cannot take and trims the one it can, so a name is
        // refused here, before anything is constructed or recorded, and is never stored under a different spelling
        // than the one the script goes on to look up. The name an item already carries is not checked again, so an
        // item named by an import can still be replaced without being renamed.
        public static void ValidateFormat(string name, string argumentName, string allowedExistingName)
        {
            ValidateFormat(name, argumentName, allowedExistingName, null);
        }
        public static void ValidateFormat(string name, string argumentName, string allowedExistingName,
                                          HashSet<char> additionalCharacters)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName, StringComparison.Ordinal))
                return;
            if (name.Trim().Length != name.Length)
                throw new PythonAPIException("The argument '" + argumentName + "' must not start or end with a " +
                                             "space: '" + name + "'.");
            //
            string error = CaeGlobals.NamedClass.CheckNameError(name, additionalCharacters);
            if (error != null) throw new PythonAPIException("The argument '" + argumentName + "' is not valid: " + error);
        }
    }
}
