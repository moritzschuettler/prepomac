// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using Python.Runtime;

namespace PrePoMax.Python
{
    // A .NET dictionary reaches Python as a wrapper that prints as its type name and is read through Keys and
    // TryGetValue, so every dictionary an API member returns is handed over as a Python dict instead. Only the
    // plain Dictionary is converted: an API object that also behaves as a map keeps its own members.
    public class PythonDictionaryEncoder : IPyObjectEncoder
    {
        // Methods
        public bool CanEncode(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
        public PyObject TryEncode(object value)
        {
            IDictionary dictionary = value as IDictionary;
            if (dictionary == null) return null;
            //
            PyDict result = new PyDict();
            foreach (DictionaryEntry entry in dictionary)
            {
                using (PyObject key = entry.Key.ToPython())
                using (PyObject item = entry.Value == null ? PyObject.None : entry.Value.ToPython())
                {
                    result.SetItem(key, item);
                }
            }
            return result;
        }
    }
}
