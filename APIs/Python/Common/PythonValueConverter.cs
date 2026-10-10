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
            return new EquationString(ToRoundTripString(ToDouble(value, argumentName)));
        }
        // The shortest text that reads back as the same double. The default format of .NET Framework keeps 15 digits,
        // so a value a journal writes as a round-trip literal would replay one unit in the last place off, and "R"
        // itself misses a few values on .NET Framework, which "G17" then writes exactly.
        public static string ToRoundTripString(double value)
        {
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (double.IsNaN(value) || double.IsInfinity(value)) return text;
            //
            double parsed;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed == value)
                return text;
            //
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
        public static double ToDouble(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            // A string is not read as a number: no equation is evaluated here, and '10' is text in Python
            if (value is bool || value is string)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a number.");
            //
            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' must be a number.");
            }
        }
        public static int ToInt(object value, string argumentName)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            // A string is not read as a number, as in ToDouble
            if (value is bool || value is string)
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
            catch (PythonAPIException)
            {
                throw;
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
                                string itemName = argumentName + "[" + i + "]";
                                if (item.IsNone() || typeName.As<string>() == "bool")
                                    throw new PythonAPIException("The item '" + itemName +
                                                                 "' must be a number or an equation string.");
                                object component;
                                try
                                {
                                    component = PyString.IsStringType(item) ? (object)item.As<string>()
                                                                            : item.As<double>();
                                }
                                catch (Exception)
                                {
                                    throw new PythonAPIException("The item '" + itemName +
                                                                 "' must be a number or an equation string.");
                                }
                                equations.Add(ToEquationString(component, itemName));
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
                int index = 0;
                foreach (object item in items)
                {
                    equations.Add(ToEquationString(item, argumentName + "[" + index + "]"));
                    index++;
                }
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
        // A typed .NET parameter refuses None and a float for an int before the call reaches the API, with a message
        // that names no argument, so an optional one is taken as an object and None stands for its default
        public static bool ToBoolOrDefault(object value, bool defaultValue, string argumentName)
        {
            if (value == null) return defaultValue;
            //
            return ToBool(value, argumentName);
        }
        public static int ToIntOrDefault(object value, int defaultValue, string argumentName)
        {
            if (value == null) return defaultValue;
            //
            return ToInt(value, argumentName);
        }
        // A typed .NET parameter refuses a value of another kind with 'No method matches given arguments', which names
        // no argument, so a name, an option and an API object are taken as an object and converted here
        public static string ToText(object value, string argumentName, bool allowNone)
        {
            if (value == null)
            {
                if (allowNone) return null;
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            }
            //
            string text = value as string;
            if (text != null) return text;
            //
            PyObject python = value as PyObject;
            if (python != null)
            {
                using (Py.GIL())
                {
                    if (PyString.IsStringType(python)) return python.As<string>();
                }
            }
            //
            throw new PythonAPIException("The argument '" + argumentName + "' must be a string, not " +
                                         DescribeKind(value) + ".");
        }
        public static string ToTextOrDefault(object value, string defaultValue, string argumentName)
        {
            if (value == null) return defaultValue;
            //
            return ToText(value, argumentName, false);
        }
        public static T ToApiObject<T>(object value, string argumentName, bool allowNone) where T : class
        {
            if (value == null)
            {
                if (allowNone) return null;
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None. It takes " +
                                             DescribeApiType(typeof(T)) + ".");
            }
            //
            T apiObject = value as T;
            if (apiObject != null) return apiObject;
            //
            throw new PythonAPIException("The argument '" + argumentName + "' takes " + DescribeApiType(typeof(T)) +
                                         ", not " + DescribeKind(value) + ".");
        }
        private static string DescribeApiType(Type type)
        {
            if (type == typeof(SelectionAPI))
                return "a Selection made by pmx.create.selection_by_ids, selection_by_pick, selection_by_area, " +
                       "selection_by_box or selection_all";
            if (type == typeof(SplitToolAPI)) return "a SplitTool made by one of the pmx.create.split_tool_* methods";
            if (type == typeof(ResultFieldOutputFilterAPI))
                return "a FieldOutputFilter made by create_median_filter, create_average_filter or " +
                       "create_smooth_filter of the result field outputs";
            if (type == typeof(ResultHistoryOutputFilterAPI))
                return "a HistoryOutputFilter made by create_minimum_filter, create_maximum_filter, " +
                       "create_sum_filter or create_average_filter of the result history outputs";
            if (type == typeof(ResultFieldOutputDraftAPI))
                return "a field output draft made by one of the create_field_output_* methods of the result " +
                       "field outputs";
            if (type == typeof(ResultHistoryOutputDraftAPI))
                return "a history output draft made by one of the create_history_output_* methods of the result " +
                       "history outputs";
            //
            return "a " + ToPythonTypeName(type);
        }
        private static string DescribeKind(object value)
        {
            if (value is string) return "str";
            if (value is bool) return "bool";
            if (value is int || value is long || value is short || value is byte || value is uint || value is ulong)
                return "int";
            if (value is double || value is float || value is decimal) return "float";
            //
            PyObject python = value as PyObject;
            if (python != null)
            {
                using (Py.GIL())
                using (PyObject type = python.GetPythonType())
                using (PyObject typeName = type.GetAttr("__name__"))
                {
                    return typeName.As<string>();
                }
            }
            //
            return ToPythonTypeName(value.GetType());
        }
        private static string ToPythonTypeName(Type type)
        {
            string name = type.Name;
            if (name.EndsWith("API") && name.Length > 3) name = name.Substring(0, name.Length - 3);
            //
            return name;
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
        // An argument the vocabulary narrows for every member that takes it lists only the values it accepts
        public static TEnum ToEnum<TEnum>(string value, string argumentName) where TEnum : struct
        {
            string[] possibleNames = null;
            if (PythonEnumVocabulary.GetType(null, null, argumentName) == typeof(TEnum))
                possibleNames = PythonEnumVocabulary.GetNarrowedValues(null, null, argumentName);
            return ToEnum<TEnum>(value, argumentName, possibleNames);
        }
        // A member that accepts only some values of the enumeration names only those when it refuses an unknown
        // value. A known value it does not accept is still parsed, so that the member refuses it with the reason
        public static TEnum ToEnum<TEnum>(string value, string argumentName, string[] possibleNames)
            where TEnum : struct
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            // Enum.TryParse also takes a number and a comma separated list, which it combines into another member,
            // so only a member name is accepted; ToFlagsEnum takes a list
            string trimmed = value.Trim();
            foreach (string name in Enum.GetNames(typeof(TEnum)))
            {
                if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
                    return (TEnum)Enum.Parse(typeof(TEnum), name);
            }
            //
            if (possibleNames == null) possibleNames = Enum.GetNames(typeof(TEnum));
            throw new PythonAPIException("The value '" + value + "' is not supported for the argument '" +
                                         argumentName + "'. Possible values are " +
                                         string.Join(", ", possibleNames) + ".");
        }
    }
}
