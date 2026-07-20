// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    internal static class PythonIntListMapConverter
    {
        // Methods
        public static SortedDictionary<int, int[]> ToIntListMap(object value, string argumentName)
        {
            if (value == null) throw InvalidMap(argumentName);
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null) return ConvertPythonDictionary(pyObject, argumentName);
            //
            IDictionary dictionary = value as IDictionary;
            if (dictionary != null) return ConvertManagedDictionary(dictionary, argumentName);
            //
            throw InvalidMap(argumentName);
        }
        private static SortedDictionary<int, int[]> ConvertPythonDictionary(PyObject value, string argumentName)
        {
            SortedDictionary<int, int[]> converted = new SortedDictionary<int, int[]>();
            try
            {
                using (Py.GIL())
                {
                    if (value.IsNone() || !PyDict.IsDictType(value)) throw InvalidMap(argumentName);
                    using (PyDict dictionary = new PyDict(value))
                    using (PyObject keys = dictionary.Keys())
                    {
                        long count = keys.Length();
                        for (int i = 0; i < count; i++)
                        {
                            using (PyObject keyObject = keys.GetItem(i))
                            {
                                int key;
                                try { key = keyObject.As<int>(); }
                                catch
                                {
                                    throw new PythonAPIException("All keys in '" + argumentName +
                                                                 "' must be part IDs.");
                                }
                                if (key <= 0)
                                    throw new PythonAPIException("All part IDs in '" + argumentName +
                                                                 "' must be greater than 0.");
                                using (PyObject valueObject = dictionary.GetItem(keyObject))
                                {
                                    converted.Add(key, PythonIntListConverter.ToIntArray(valueObject,
                                                                                         argumentName + "[" + key + "]"));
                                }
                            }
                        }
                    }
                }
            }
            catch (PythonAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName +
                                             "' could not be converted to dict[int, list[int]]: " + ex.Message);
            }
            if (converted.Count == 0) throw InvalidMap(argumentName);
            return converted;
        }
        private static SortedDictionary<int, int[]> ConvertManagedDictionary(IDictionary value, string argumentName)
        {
            SortedDictionary<int, int[]> converted = new SortedDictionary<int, int[]>();
            foreach (DictionaryEntry entry in value)
            {
                int key;
                try { key = Convert.ToInt32(entry.Key); }
                catch
                {
                    throw new PythonAPIException("All keys in '" + argumentName + "' must be part IDs.");
                }
                if (key <= 0)
                    throw new PythonAPIException("All part IDs in '" + argumentName + "' must be greater than 0.");
                converted.Add(key, PythonIntListConverter.ToIntArray(entry.Value, argumentName + "[" + key + "]"));
            }
            if (converted.Count == 0) throw InvalidMap(argumentName);
            return converted;
        }
        private static PythonAPIException InvalidMap(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-empty dict[int, list[int]].");
        }
    }
}
