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
using System.Linq;

namespace APIs.Python
{
    internal static class PythonIntListConverter
    {
        // Methods
        public static int[] ToIntArray(object value, string argumentName)
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
        private static int[] ConvertPythonIterable(PyObject value, string argumentName)
        {
            List<int> converted = new List<int>();
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
                                try
                                {
                                    converted.Add(item.As<int>());
                                }
                                catch
                                {
                                    throw new PythonAPIException("All items in '" + argumentName +
                                                                 "' must be integers.");
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
                                             "' could not be converted to list[int]: " + ex.Message);
            }
            return Normalize(converted, argumentName);
        }
        private static int[] ConvertManagedEnumerable(IEnumerable value, string argumentName)
        {
            List<int> converted = new List<int>();
            foreach (object item in value)
            {
                if (item is bool || !(item is sbyte || item is byte || item is short || item is ushort ||
                                      item is int || item is uint || item is long || item is ulong))
                    throw new PythonAPIException("All items in '" + argumentName + "' must be integers.");
                try
                {
                    converted.Add(Convert.ToInt32(item));
                }
                catch
                {
                    throw new PythonAPIException("All items in '" + argumentName +
                                                 "' must be valid 32-bit integers.");
                }
            }
            return Normalize(converted, argumentName);
        }
        private static int[] Normalize(IEnumerable<int> values, string argumentName)
        {
            int[] ids = values.Distinct().OrderBy(id => id).ToArray();
            if (ids.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain at least one ID.");
            if (ids.Any(id => id <= 0))
                throw new PythonAPIException("All IDs in '" + argumentName + "' must be greater than 0.");
            return ids;
        }
        private static PythonAPIException InvalidList(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-null list[int].");
        }
    }
}
