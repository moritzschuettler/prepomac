// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System;

namespace APIs.Python
{
    // A result output finds its source field and components regardless of case, as the result keys its user-defined
    // field outputs, and stores them in the spelling of the result, because the result looks a field read from the
    // result file and every component up by its exact name. An exact spelling wins over another one that differs
    // only in case.
    internal static class ResultFieldNameResolver
    {
        // Methods
        public static string GetStoredFieldName(FeResults result, string fieldName)
        {
            if (fieldName == null) return null;
            if (result.ContainsResultFieldOutput(fieldName)) return result.GetResultFieldOutput(fieldName).Name;
            //
            return FindStoredName(result.GetAllFieldNames(), fieldName);
        }
        public static string GetStoredComponentName(FeResults result, string storedFieldName, string componentName)
        {
            if (storedFieldName == null || componentName == null) return null;
            //
            return FindStoredName(result.GetFieldComponentNames(storedFieldName), componentName);
        }
        private static string FindStoredName(string[] storedNames, string name)
        {
            if (storedNames == null) return null;
            //
            foreach (string storedName in storedNames)
            {
                if (storedName == name) return storedName;
            }
            foreach (string storedName in storedNames)
            {
                if (string.Equals(storedName, name, StringComparison.OrdinalIgnoreCase)) return storedName;
            }
            return null;
        }
    }
}
