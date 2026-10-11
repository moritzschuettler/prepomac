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

namespace APIs.Python
{
    internal static class PythonDoubleListConverter
    {
        // Methods
        public static double[] ToDoubleArray(object value, string argumentName)
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
        public static double[][] ToDoubleArray2D(object value, string argumentName)
        {
            if (value == null || value is string) throw InvalidList2D(argumentName);
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null) return ConvertPythonRows(pyObject, argumentName);
            //
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                List<double[]> rows = new List<double[]>();
                foreach (object item in enumerable) rows.Add(ToDoubleArray(item, argumentName));
                return rows.ToArray();
            }
            //
            throw InvalidList2D(argumentName);
        }
        public static double[] ToFiniteDoubleArray(object value, string argumentName)
        {
            double[] values = ToDoubleArray(value, argumentName);
            RefuseNonFinite(values, argumentName);
            //
            return values;
        }
        public static double[][] ToFiniteDoubleArray2D(object value, string argumentName)
        {
            double[][] rows = ToDoubleArray2D(value, argumentName);
            foreach (double[] row in rows) RefuseNonFinite(row, argumentName);
            //
            return rows;
        }
        private static void RefuseNonFinite(double[] values, string argumentName)
        {
            if (values == null) return;
            //
            foreach (double value in values)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new PythonAPIException("The argument '" + argumentName + "' must contain finite numbers, " +
                                                 "but it contains " + value.ToString(CultureInfo.InvariantCulture) +
                                                 ".");
            }
        }
        private static double[] ConvertPythonIterable(PyObject value, string argumentName)
        {
            List<double> converted = new List<double>();
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
                                // A bool converts to 1 or 0, which is never what a script meant. True and False
                                // are the only bools, so an int or a float is read without asking for its type name
                                if (item.Handle == pythonTrue.Handle || item.Handle == pythonFalse.Handle)
                                    throw InvalidNumber(argumentName);
                                if (!PyFloat.IsFloatType(item) && !PyInt.IsIntType(item))
                                {
                                    if (PyString.IsStringType(item)) throw InvalidNumber(argumentName);
                                    using (PyObject type = item.GetPythonType())
                                    using (PyObject typeName = type.GetAttr("__name__"))
                                    {
                                        if (typeName.As<string>() == "bool") throw InvalidNumber(argumentName);
                                    }
                                }
                                try
                                {
                                    converted.Add(item.As<double>());
                                }
                                catch
                                {
                                    throw InvalidNumber(argumentName);
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
                                             "' could not be converted to list[float]: " + ex.Message);
            }
            return converted.ToArray();
        }
        private static double[][] ConvertPythonRows(PyObject value, string argumentName)
        {
            List<double[]> rows = new List<double[]>();
            try
            {
                using (Py.GIL())
                {
                    if (value.IsNone() || PyString.IsStringType(value)) throw InvalidList2D(argumentName);
                    using (PyObject builtins = Py.Import("builtins"))
                    using (PyObject listFunction = builtins.GetAttr("list"))
                    using (PyObject items = listFunction.Invoke(value))
                    {
                        long count = items.Length();
                        for (int i = 0; i < count; i++)
                        {
                            using (PyObject item = items.GetItem(i))
                            {
                                rows.Add(ConvertPythonIterable(item, argumentName));
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
                                             "' could not be converted to list[list[float]]: " + ex.Message);
            }
            return rows.ToArray();
        }
        private static double[] ConvertManagedEnumerable(IEnumerable value, string argumentName)
        {
            List<double> converted = new List<double>();
            foreach (object item in value)
            {
                if (item is bool || item is string || !(item is sbyte || item is byte || item is short ||
                                                        item is ushort || item is int || item is uint ||
                                                        item is long || item is ulong || item is float ||
                                                        item is double || item is decimal))
                    throw InvalidNumber(argumentName);
                try
                {
                    converted.Add(Convert.ToDouble(item, CultureInfo.InvariantCulture));
                }
                catch
                {
                    throw InvalidNumber(argumentName);
                }
            }
            return converted.ToArray();
        }
        private static PythonAPIException InvalidNumber(string argumentName)
        {
            return new PythonAPIException("All items in '" + argumentName + "' must be numbers.");
        }
        private static PythonAPIException InvalidList(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName + "' must be a non-null list[float].");
        }
        private static PythonAPIException InvalidList2D(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName +
                                          "' must be a non-null list of rows, each a list[float].");
        }
    }
}
