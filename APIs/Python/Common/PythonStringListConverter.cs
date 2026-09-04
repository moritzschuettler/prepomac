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
    internal static class PythonStringListConverter
    {
        // Methods
        // The model collections accept a single name as well as a list of names
        public static string[] ToStringArrayOrSingle(object value, string argumentName)
        {
            string single = value as string;
            if (single != null) return new string[] { single };
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null)
            {
                using (Py.GIL())
                {
                    if (PyString.IsStringType(pyObject)) return new string[] { pyObject.ToString() };
                }
            }
            //
            return ToStringArray(value, argumentName);
        }
        public static string[] ToStringArray(object value, string argumentName)
        {
            if (value == null || value is string) throw InvalidList(argumentName);
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null) return ConvertPythonIterable(pyObject, argumentName);
            //
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null) return ConvertManagedEnumerable(enumerable, argumentName);
            //
            throw InvalidList(argumentName);
        }
        private static string[] ConvertPythonIterable(PyObject value, string argumentName)
        {
            List<string> converted = new List<string>();
            try
            {
                using (Py.GIL())
                {
                    if (value.IsNone() || PyString.IsStringType(value)) throw InvalidList(argumentName);
                    using (PyObject builtins = Py.Import("builtins"))
                    using (PyObject listFunction = builtins.GetAttr("list"))
                    using (PyObject items = listFunction.Invoke(value))
                    {
                        long count = items.Length();
                        for (int i = 0; i < count; i++)
                        {
                            using (PyObject item = items.GetItem(i))
                            {
                                if (!PyString.IsStringType(item))
                                    throw new PythonAPIException("All items in '" + argumentName +
                                                                 "' must be strings.");
                                converted.Add(item.As<string>());
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
                                             "' could not be converted to list[str]: " + ex.Message);
            }
            return converted.ToArray();
        }
        private static string[] ConvertManagedEnumerable(IEnumerable value, string argumentName)
        {
            List<string> converted = new List<string>();
            foreach (object item in value)
            {
                string text = item as string;
                if (text == null)
                    throw new PythonAPIException("All items in '" + argumentName + "' must be strings.");
                converted.Add(text);
            }
            return converted.ToArray();
        }
        private static PythonAPIException InvalidList(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-null list[str].");
        }
    }
}
