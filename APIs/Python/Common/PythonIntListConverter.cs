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
using System.Globalization;
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
                    using (PyObject pythonTrue = true.ToPython())
                    using (PyObject pythonFalse = false.ToPython())
                    {
                        long count = items.Length();
                        for (int i = 0; i < count; i++)
                        {
                            using (PyObject item = items.GetItem(i))
                            {
                                converted.Add(ToWholeNumberWithGIL(item, pythonTrue.Handle, pythonFalse.Handle,
                                                                   argumentName));
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
            foreach (object item in value) converted.Add(ToWholeNumber(item, argumentName));
            return Normalize(converted, argumentName);
        }
        // One rule for every whole number argument, as PythonValueConverter.ToInt reads it: a float that is whole is
        // taken, a fraction, a bool, a string and a number outside 32 bits are refused
        private static int ToWholeNumber(PyObject item, string argumentName)
        {
            using (Py.GIL())
            using (PyObject pythonTrue = true.ToPython())
            using (PyObject pythonFalse = false.ToPython())
            {
                return ToWholeNumberWithGIL(item, pythonTrue.Handle, pythonFalse.Handle, argumentName);
            }
        }
        // True and False are the only bools, so a bool is told by its handle, and an int or a float is read
        // without asking its type for its name
        private static int ToWholeNumberWithGIL(PyObject item, IntPtr trueHandle, IntPtr falseHandle,
                                                string argumentName)
        {
            double number;
            try
            {
                if (item.Handle == trueHandle || item.Handle == falseHandle) throw NotWholeNumbers(argumentName);
                if (PyInt.IsIntType(item))
                {
                    long wholeNumber = item.As<long>();
                    if (wholeNumber < int.MinValue || wholeNumber > int.MaxValue) throw NotWholeNumbers(argumentName);
                    return (int)wholeNumber;
                }
                if (PyFloat.IsFloatType(item)) number = item.As<double>();
                else
                {
                    if (item.IsNone() || PyString.IsStringType(item)) throw NotWholeNumbers(argumentName);
                    using (PyObject type = item.GetPythonType())
                    using (PyObject typeName = type.GetAttr("__name__"))
                    {
                        if (typeName.As<string>() == "bool") throw NotWholeNumbers(argumentName);
                    }
                    number = item.As<double>();
                }
            }
            catch (PythonAPIException)
            {
                throw;
            }
            catch (Exception)
            {
                throw NotWholeNumbers(argumentName);
            }
            return ToWholeNumberOfDouble(number, argumentName);
        }
        public static int ToWholeNumber(object item, string argumentName)
        {
            PyObject python = item as PyObject;
            if (python != null) return ToWholeNumber(python, argumentName);
            //
            if (item is bool || !(item is sbyte || item is byte || item is short || item is ushort || item is int ||
                                  item is uint || item is long || item is ulong || item is float || item is double ||
                                  item is decimal))
                throw NotWholeNumbers(argumentName);
            //
            double number;
            try
            {
                number = Convert.ToDouble(item, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw NotWholeNumbers(argumentName);
            }
            return ToWholeNumberOfDouble(number, argumentName);
        }
        private static int ToWholeNumberOfDouble(double number, string argumentName)
        {
            if (double.IsNaN(number) || double.IsInfinity(number) || number != Math.Truncate(number) ||
                number < int.MinValue || number > int.MaxValue)
                throw NotWholeNumbers(argumentName);
            //
            return (int)number;
        }
        private static PythonAPIException NotWholeNumbers(string argumentName)
        {
            return new PythonAPIException("All items in '" + argumentName + "' must be whole 32-bit numbers, not " +
                                          "fractions, bools or strings.");
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
