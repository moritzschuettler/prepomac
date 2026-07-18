// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using Python.Runtime;
using System;
using System.Collections;
using System.Globalization;

namespace PrePoMax.API.Python
{
    internal static class PythonDictionaryConverter
    {
        // Methods
        public static OrderedDictionary<string, double> ToStringDoubleDictionary(object value, string argumentName)
        {
            if (value == null) throw InvalidDictionary(argumentName);
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null) return ConvertPythonDictionary(pyObject, argumentName);
            //
            IDictionary dictionary = value as IDictionary;
            if (dictionary != null) return ConvertManagedDictionary(dictionary, argumentName);
            //
            throw InvalidDictionary(argumentName);
        }
        private static OrderedDictionary<string, double> ConvertPythonDictionary(PyObject value, string argumentName)
        {
            OrderedDictionary<string, double> converted = new OrderedDictionary<string, double>("StringDoubleDictionary");
            try
            {
                using (Py.GIL())
                {
                    if (value.IsNone() || !PyDict.IsDictType(value)) throw InvalidDictionary(argumentName);
                    using (PyDict dictionary = new PyDict(value))
                    using (PyObject keys = dictionary.Keys())
                    {
                        long count = keys.Length();
                        for (int i = 0; i < count; i++)
                        {
                            using (PyObject keyObject = keys.GetItem(i))
                            {
                                if (!PyString.IsStringType(keyObject))
                                    throw new PythonAPIException("All keys in '" + argumentName + "' must be strings.");
                                string key = keyObject.As<string>();
                                using (PyObject valueObject = dictionary.GetItem(keyObject))
                                {
                                    double itemValue;
                                    try
                                    {
                                        itemValue = valueObject.As<double>();
                                    }
                                    catch
                                    {
                                        throw new PythonAPIException("The value for item '" + key + "' in '" +
                                                                     argumentName + "' must be numeric.");
                                    }
                                    converted.Add(key, itemValue);
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
                                             "' could not be converted to dict[str, float]: " + ex.Message);
            }
            return converted;
        }
        private static OrderedDictionary<string, double> ConvertManagedDictionary(IDictionary value, string argumentName)
        {
            OrderedDictionary<string, double> converted = new OrderedDictionary<string, double>("StringDoubleDictionary");
            foreach (DictionaryEntry entry in value)
            {
                string key = entry.Key as string;
                if (key == null)
                    throw new PythonAPIException("All keys in '" + argumentName + "' must be strings.");
                try
                {
                    converted.Add(key, Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture));
                }
                catch
                {
                    throw new PythonAPIException("The value for item '" + key + "' in '" + argumentName +
                                                 "' must be numeric.");
                }
            }
            return converted;
        }
        private static PythonAPIException InvalidDictionary(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-null dict[str, float].");
        }
    }
}
