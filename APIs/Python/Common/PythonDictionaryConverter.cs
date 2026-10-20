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

namespace APIs.Python
{
    internal static class PythonDictionaryConverter
    {
        // Methods
        // The element types of a part are named values rather than numbers, so they need their own form
        public static CaeGlobals.OrderedDictionary<string, string> ToStringStringDictionary(object value, string argumentName)
        {
            CaeGlobals.OrderedDictionary<string, string> converted =
                new CaeGlobals.OrderedDictionary<string, string>("StringStringDictionary", StringComparer.OrdinalIgnoreCase);
            if (value == null) throw InvalidDictionary(argumentName);
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null)
            {
                try
                {
                    using (Py.GIL())
                    {
                        if (pyObject.IsNone() || !PyDict.IsDictType(pyObject)) throw InvalidDictionary(argumentName);
                        using (PyDict dictionary = new PyDict(pyObject))
                        using (PyObject keys = dictionary.Keys())
                        {
                            long count = keys.Length();
                            for (int i = 0; i < count; i++)
                            {
                                using (PyObject keyObject = keys.GetItem(i))
                                using (PyObject valueObject = dictionary.GetItem(keyObject))
                                {
                                    if (!PyString.IsStringType(keyObject))
                                        throw new PythonAPIException("All keys in '" + argumentName +
                                                                     "' must be strings.");
                                    AddOnce(converted, keyObject.As<string>(), valueObject.ToString(),
                                            argumentName);
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
                                                 "' could not be converted to dict[str, str]: " + ex.Message);
                }
                return converted;
            }
            //
            IDictionary managed = value as IDictionary;
            if (managed == null) throw InvalidDictionary(argumentName);
            foreach (DictionaryEntry entry in managed)
            {
                string key = entry.Key as string;
                if (key == null)
                    throw new PythonAPIException("All keys in '" + argumentName + "' must be strings.");
                AddOnce(converted, key, entry.Value == null ? null : entry.Value.ToString(), argumentName);
            }
            //
            return converted;
        }
        public static CaeGlobals.OrderedDictionary<string, double> ToStringDoubleDictionary(object value, string argumentName)
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
        private static CaeGlobals.OrderedDictionary<string, double> ConvertPythonDictionary(PyObject value, string argumentName)
        {
            CaeGlobals.OrderedDictionary<string, double> converted =
                new CaeGlobals.OrderedDictionary<string, double>("StringDoubleDictionary", StringComparer.OrdinalIgnoreCase);
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
                                        // A bool converts to 1 or 0, which is never what a script meant
                                        using (PyObject type = valueObject.GetPythonType())
                                        using (PyObject typeName = type.GetAttr("__name__"))
                                        {
                                            if (typeName.As<string>() == "bool") throw new InvalidCastException();
                                        }
                                        itemValue = valueObject.As<double>();
                                    }
                                    catch
                                    {
                                        throw new PythonAPIException("The value for item '" + key + "' in '" +
                                                                     argumentName + "' must be numeric.");
                                    }
                                    AddOnce(converted, key, itemValue, argumentName);
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
        private static CaeGlobals.OrderedDictionary<string, double> ConvertManagedDictionary(IDictionary value, string argumentName)
        {
            CaeGlobals.OrderedDictionary<string, double> converted =
                new CaeGlobals.OrderedDictionary<string, double>("StringDoubleDictionary", StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in value)
            {
                string key = entry.Key as string;
                if (key == null)
                    throw new PythonAPIException("All keys in '" + argumentName + "' must be strings.");
                double itemValue;
                try
                {
                    if (entry.Value is bool) throw new InvalidCastException();
                    itemValue = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new PythonAPIException("The value for item '" + key + "' in '" + argumentName +
                                                 "' must be numeric.");
                }
                AddOnce(converted, key, itemValue, argumentName);
            }
            return converted;
        }
        // The keys name model items, which the model compares regardless of case, so two keys that differ only
        // in case name the same item twice
        private static void AddOnce<TValue>(CaeGlobals.OrderedDictionary<string, TValue> converted, string key, TValue value,
                                            string argumentName)
        {
            if (converted.ContainsKey(key))
                throw new PythonAPIException("The argument '" + argumentName + "' names '" + key + "' more than " +
                                             "once; names are compared regardless of case.");
            //
            converted.Add(key, value);
        }
        private static PythonAPIException InvalidDictionary(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-null dict[str, float].");
        }
    }
}
