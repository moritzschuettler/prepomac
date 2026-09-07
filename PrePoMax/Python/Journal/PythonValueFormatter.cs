// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using CaeGlobals;

namespace PrePoMax.Python.Journal
{
    public static class PythonValueFormatter
    {
        // Methods                                                                                                                  
        public static string Format(object value)
        {
            if (value == null) return "None";
            //
            if (value is string text) return Text(text);
            if (value is bool boolValue) return boolValue ? "True" : "False";
            if (value is double doubleValue) return Number(doubleValue);
            if (value is float floatValue) return Number(floatValue);
            if (value is int || value is long || value is short || value is byte)
                return Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture);
            if (value is EquationContainer equationContainer) return Equation(equationContainer);
            if (value is EquationString equationString) return Text(WithoutEquationMark(equationString.Equation));
            if (value is Enum) return Text(value.ToString());
            if (value is Color color) return Channels(color);
            if (value is IDictionary dictionary) return Dictionary(dictionary);
            if (value is IEnumerable items) return List(items);
            //
            throw new NotSupportedException("A value of type " + value.GetType().Name + " has no Python form.");
        }
        public static string Text(string value)
        {
            if (value == null) return "None";
            //
            StringBuilder builder = new StringBuilder(value.Length + 2);
            builder.Append('\'');
            foreach (char character in value)
            {
                if (character == '\\') builder.Append("\\\\");
                else if (character == '\'') builder.Append("\\'");
                else if (character == '\n') builder.Append("\\n");
                else if (character == '\r') builder.Append("\\r");
                else if (character == '\t') builder.Append("\\t");
                else builder.Append(character);
            }
            builder.Append('\'');
            //
            return builder.ToString();
        }
        public static string Path(string value)
        {
            if (value == null) return "None";
            // A raw string keeps a Windows path readable, but it cannot end in a backslash and cannot
            // carry the quote character it is delimited with
            if (value.IndexOf('\\') >= 0 && !value.EndsWith("\\") && value.IndexOf('\'') < 0)
                return "r'" + value + "'";
            //
            return Text(value);
        }
        public static string Number(double value)
        {
            // A degree of freedom that is not constrained carries a NaN
            if (double.IsNaN(value)) return "None";
            if (double.IsInfinity(value))
                throw new NotSupportedException("An infinite value has no Python form.");
            //
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ||
                parsed != value)
                text = value.ToString("R", CultureInfo.InvariantCulture);
            // Keep the literal unambiguously a float, so that it never reaches an integer overload
            if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0) text += ".0";
            //
            return text;
        }
        public static string Equation(EquationContainer value)
        {
            if (value == null) return "None";
            // The API takes a string as an equation and a number as a value in model units
            if (value.IsEquation()) return Text(WithoutEquationMark(value.String));
            //
            return Number(value.Value);
        }
        // The output variable enums are used as flags although they carry no FlagsAttribute, so a
        // combined value has no name of its own and the API takes the list of names instead
        public static string Flags(Enum value)
        {
            if (value == null) return "None";
            //
            Type enumType = value.GetType();
            long combined = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            List<string> names = new List<string>();
            foreach (object entry in Enum.GetValues(enumType))
            {
                long flag = Convert.ToInt64(entry, CultureInfo.InvariantCulture);
                if (flag != 0 && (combined & flag) == flag) names.Add(entry.ToString());
            }
            if (names.Count == 1) return Text(names[0]);
            //
            return List(names);
        }
        public static string Channels(Color value)
        {
            if (value.A == 255) return "[" + value.R + ", " + value.G + ", " + value.B + "]";
            //
            return "[" + value.R + ", " + value.G + ", " + value.B + ", " + value.A + "]";
        }
        public static string List(IEnumerable values)
        {
            if (values == null) return "None";
            //
            StringBuilder builder = new StringBuilder("[");
            bool first = true;
            foreach (object item in values)
            {
                if (!first) builder.Append(", ");
                builder.Append(Format(item));
                first = false;
            }
            builder.Append("]");
            //
            return builder.ToString();
        }
        public static string Dictionary(IDictionary values)
        {
            if (values == null) return "None";
            //
            StringBuilder builder = new StringBuilder("{");
            bool first = true;
            foreach (DictionaryEntry entry in values)
            {
                if (!first) builder.Append(", ");
                builder.Append(Format(entry.Key));
                builder.Append(": ");
                builder.Append(Format(entry.Value));
                first = false;
            }
            builder.Append("}");
            //
            return builder.ToString();
        }
        public static string Argument(string name, string formattedValue)
        {
            return name + "=" + formattedValue;
        }
        private static string WithoutEquationMark(string equation)
        {
            if (equation != null && equation.StartsWith("=")) return equation.Substring(1);
            //
            return equation;
        }
    }
}
