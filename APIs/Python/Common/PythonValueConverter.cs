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
using System.Collections.Generic;
using System.Globalization;

namespace APIs.Python
{
    // The single place that turns a Python argument into a model value. A number is a value in the
    // current model units; a string is an equation evaluated by the application.
    internal static class PythonValueConverter
    {
        // Methods
        public static EquationString ToEquationString(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();
                if (text.Length == 0)
                    throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
                // The application marks an equation with a leading equals sign
                if (!text.StartsWith("=")) text = "=" + text;
                return new EquationString(text);
            }
            //
            return new EquationString(ToDouble(value, argumentName).ToString(CultureInfo.InvariantCulture));
        }
        public static double ToDouble(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            if (value is bool)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a number.");
            //
            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' must be a number or an equation string.");
            }
        }
        public static int ToInt(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            if (value is bool)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a whole number.");
            //
            try
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number) || number != Math.Truncate(number) ||
                    number < int.MinValue || number > int.MaxValue)
                    throw new PythonAPIException("The argument '" + argumentName +
                                                 "' must be a whole 32-bit number.");
                return (int)number;
            }
            catch (Exception)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' must be a whole number.");
            }
        }
        public static EquationString[] ToEquationArray(object value, string argumentName, int length)
        {
            if (value == null || value is string)
                throw new PythonAPIException("The argument '" + argumentName +
                                             "' must be a list of values or equations.");
            //
            List<EquationString> equations = new List<EquationString>();
            PyObject python = value as PyObject;
            if (python != null)
            {
                using (Py.GIL())
                {
                    if (python.IsNone() || PyString.IsStringType(python))
                        throw new PythonAPIException("The argument '" + argumentName + "' must be a list.");
                    using (PyObject builtins = Py.Import("builtins"))
                    using (PyObject listFunction = builtins.GetAttr("list"))
                    using (PyObject items = listFunction.Invoke(python))
                    {
                        for (int i = 0; i < items.Length(); i++)
                        {
                            using (PyObject item = items.GetItem(i))
                            using (PyObject type = item.GetPythonType())
                            using (PyObject typeName = type.GetAttr("__name__"))
                            {
                                if (item.IsNone() || typeName.As<string>() == "bool")
                                    throw new PythonAPIException("The items of '" + argumentName +
                                                                 "' must be numbers or equation strings.");
                                object component = PyString.IsStringType(item)
                                                   ? (object)item.As<string>() : item.As<double>();
                                equations.Add(ToEquationString(component, argumentName + "[" + i + "]"));
                            }
                        }
                    }
                }
            }
            else
            {
                IEnumerable items = value as IEnumerable;
                if (items == null)
                    throw new PythonAPIException("The argument '" + argumentName + "' must be a list.");
                foreach (object item in items) equations.Add(ToEquationString(item, argumentName));
            }
            if (equations.Count != length)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain " + length +
                                             " components.");
            //
            return equations.ToArray();
        }
        public static double ToFiniteDouble(object value, string argumentName)
        {
            double number = ToDouble(value, argumentName);
            if (double.IsNaN(number) || double.IsInfinity(number))
                throw new PythonAPIException("The argument '" + argumentName + "' must be finite.");
            //
            return number;
        }
        public static bool ToBool(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            if (value is bool) return (bool)value;
            //
            throw new PythonAPIException("The argument '" + argumentName + "' must be True or False.");
        }
        // The output variable enums are used as flags, although they carry no FlagsAttribute, so a
        // combined value is neither defined nor printable by Enum.ToString
        public static TEnum ToFlagsEnum<TEnum>(object value, string argumentName) where TEnum : struct
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(value, argumentName);
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            long combined = 0;
            foreach (string name in names)
                combined |= Convert.ToInt64(ToEnum<TEnum>(name, argumentName), CultureInfo.InvariantCulture);
            //
            return (TEnum)Enum.ToObject(typeof(TEnum), combined);
        }
        public static string[] FromFlagsEnum(Type enumType, object value)
        {
            long combined = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            List<string> names = new List<string>();
            foreach (object entry in Enum.GetValues(enumType))
            {
                long flag = Convert.ToInt64(entry, CultureInfo.InvariantCulture);
                if (flag != 0 && (combined & flag) == flag) names.Add(entry.ToString());
            }
            //
            return names.ToArray();
        }
        public static TEnum ToEnum<TEnum>(string value, string argumentName) where TEnum : struct
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            TEnum parsed;
            if (!Enum.TryParse(value.Trim(), true, out parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
                throw new PythonAPIException("The value '" + value + "' is not supported for the argument '" +
                                             argumentName + "'. Possible values are " +
                                             string.Join(", ", Enum.GetNames(typeof(TEnum))) + ".");
            //
            return parsed;
        }
    }
}
