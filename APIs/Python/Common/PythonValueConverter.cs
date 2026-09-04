// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
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
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' must be a whole number.");
            }
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
