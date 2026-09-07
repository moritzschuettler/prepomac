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

namespace PrePoMax.Python.Journal
{
    // Holds the Python variables the emitted script binds. A variable is keyed by what it stands for
    // rather than by the name the model item carries, so that a later rename keeps using the variable
    // the earlier statements bound.
    public class PythonJournalScope
    {
        // Variables                                                                                                                
        private readonly Dictionary<string, string> _variableNames;
        private readonly Dictionary<string, int> _counts;


        // Constructors                                                                                                             
        public PythonJournalScope()
        {
            _variableNames = new Dictionary<string, string>();
            _counts = new Dictionary<string, int>();
        }


        // Methods                                                                                                                  
        public bool Contains(string key)
        {
            return key != null && _variableNames.ContainsKey(key);
        }
        public string GetVariableName(string key)
        {
            if (key == null) return null;
            //
            _variableNames.TryGetValue(key, out string variableName);
            return variableName;
        }
        public string CreateVariableName(string prefix, string key)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (key == null) throw new ArgumentNullException(nameof(key));
            //
            _counts.TryGetValue(prefix, out int count);
            count++;
            _counts[prefix] = count;
            //
            string variableName = prefix + "_" + count;
            _variableNames[key] = variableName;
            //
            return variableName;
        }
        public void Rename(string oldKey, string newKey)
        {
            if (oldKey == null || newKey == null || oldKey == newKey) return;
            if (!_variableNames.TryGetValue(oldKey, out string variableName)) return;
            //
            _variableNames.Remove(oldKey);
            _variableNames[newKey] = variableName;
        }
        public void Remove(string key)
        {
            if (key != null) _variableNames.Remove(key);
        }
    }
}
