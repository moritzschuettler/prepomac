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
    // Turns an argument that accepts one detached part or a list of them into a managed list. A Python
    // list reaches an object parameter as a PyObject, not as a managed enumerable, so the list is
    // unwrapped through the Python runtime.
    internal static class PythonObjectListConverter
    {
        // Methods
        public static List<TPart> ToList<TPart>(object value, string argumentName, string expectation)
            where TPart : class
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            List<TPart> converted = new List<TPart>();
            TPart single = value as TPart;
            if (single != null)
            {
                converted.Add(single);
                return converted;
            }
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null) return ToListFromPython<TPart>(pyObject, argumentName, expectation);
            //
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null) throw Invalid(argumentName, expectation);
            //
            foreach (object item in enumerable)
            {
                TPart part = item as TPart;
                if (part == null) throw Invalid(argumentName, expectation);
                converted.Add(part);
            }
            //
            return converted;
        }
        //
        private static List<TPart> ToListFromPython<TPart>(PyObject value, string argumentName, string expectation)
            where TPart : class
        {
            List<TPart> converted = new List<TPart>();
            using (Py.GIL())
            {
                if (!PyList.IsListType(value) && !PyTuple.IsTupleType(value))
                {
                    converted.Add(Unwrap<TPart>(value, argumentName, expectation));
                    return converted;
                }
                //
                using (PyObject builtins = Py.Import("builtins"))
                using (PyObject listFunction = builtins.GetAttr("list"))
                using (PyObject items = listFunction.Invoke(value))
                {
                    long count = items.Length();
                    for (int i = 0; i < count; i++)
                    {
                        using (PyObject item = items[i])
                            converted.Add(Unwrap<TPart>(item, argumentName, expectation));
                    }
                }
            }
            //
            return converted;
        }
        private static TPart Unwrap<TPart>(PyObject value, string argumentName, string expectation)
            where TPart : class
        {
            try
            {
                return value.As<TPart>();
            }
            catch (Exception)
            {
                // The conversion failure is reported with the API wording rather than the Python one
                throw Invalid(argumentName, expectation);
            }
        }
        private static PythonAPIException Invalid(string argumentName, string expectation)
        {
            return new PythonAPIException("The argument '" + argumentName + "' must be " + expectation + ".");
        }
    }
}
