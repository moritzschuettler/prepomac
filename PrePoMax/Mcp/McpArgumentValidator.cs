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

namespace PrePoMax.Mcp
{
    // Checks the values of a tool call against the schema the tool declares, before the call is run. The
    // readers of a parsed document answer the default for a value of the wrong type, so without this a
    // "true" would be read as false and a "2" as nothing given, and the call would run as if the argument had
    // been left out. The schema is the one tools/list sends, so the check cannot drift from what the client
    // was told: the type of each value, the type of the items of an array, the allowed values of an
    // enumeration and the range of a number.
    public static class McpArgumentValidator
    {
        // Static methods                                                                                                           
        // Returns the refusal for the first argument the schema does not allow, or null when every one is allowed
        public static string Validate(McpTool tool, object arguments)
        {
            object schema;
            string error;
            if (!JsonReader.TryParse(tool.InputSchema, out schema, out error)) return null;
            Dictionary<string, object> items = arguments as Dictionary<string, object>;
            //
            List<object> required = JsonReader.GetArray(schema, "required");
            if (required != null)
            {
                foreach (object name in required)
                {
                    string requiredName = name as string;
                    if (requiredName != null && (items == null || !items.ContainsKey(requiredName)))
                        return "The tool " + tool.Name + " needs the argument " + requiredName + ". Nothing was done.";
                }
            }
            // A client may send null for an optional argument it means to leave out, and it is taken that way, so
            // the tool runs with the default the schema describes
            if (items != null)
            {
                List<string> omitted = new List<string>();
                foreach (KeyValuePair<string, object> item in items)
                {
                    if (item.Value == null && (required == null || !required.Contains(item.Key))) omitted.Add(item.Key);
                }
                foreach (string name in omitted) items.Remove(name);
            }
            //
            Dictionary<string, object> properties = JsonReader.GetObject(schema, "properties");
            if (items == null || properties == null) return null;
            Dictionary<string, object> canonical = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> item in items)
            {
                Dictionary<string, object> property = JsonReader.GetObject(properties, item.Key);
                if (property == null) continue;
                //
                string problem = GetProblem(property, item.Value);
                if (problem != null)
                    return "The argument " + item.Key + " of " + tool.Name + " " + problem + ". Nothing was done.";
                string spelling = GetEnumSpelling(property, item.Value);
                if (spelling != null && spelling != (string)item.Value) canonical.Add(item.Key, spelling);
            }
            // An option string is compared regardless of case, as the API compares it, and is passed on in the
            // spelling the schema declares
            foreach (KeyValuePair<string, object> item in canonical) items[item.Key] = item.Value;
            return null;
        }
        // The declared spelling of a string enumeration value, compared regardless of case, or null
        private static string GetEnumSpelling(Dictionary<string, object> property, object value)
        {
            string text = value as string;
            List<object> allowed = JsonReader.GetArray(property, "enum");
            if (text == null || allowed == null) return null;
            foreach (object allowedValue in allowed)
            {
                string allowedText = allowedValue as string;
                if (allowedText != null && string.Equals(allowedText, text, StringComparison.OrdinalIgnoreCase))
                    return allowedText;
            }
            return null;
        }
        private static string GetProblem(Dictionary<string, object> property, object value)
        {
            string type = JsonReader.GetString(property, "type", null);
            if (!IsOfType(value, type)) return "must be " + DescribeType(type) + ", not " + DescribeValue(value);
            //
            if (type == "array")
            {
                Dictionary<string, object> itemSchema = JsonReader.GetObject(property, "items");
                if (itemSchema == null) return null;
                List<object> values = (List<object>)value;
                for (int i = 0; i < values.Count; i++)
                {
                    string problem = GetProblem(itemSchema, values[i]);
                    if (problem != null) return "has item " + (i + 1).ToString(CultureInfo.InvariantCulture) + " that " + problem;
                }
                return null;
            }
            List<object> allowed = JsonReader.GetArray(property, "enum");
            if (allowed != null && !allowed.Contains(value) && GetEnumSpelling(property, value) == null)
            {
                List<string> names = new List<string>();
                foreach (object allowedValue in allowed) names.Add(DescribeValue(allowedValue));
                return "must be one of " + string.Join(", ", names.ToArray()) + ", not " + DescribeValue(value);
            }
            if (value is double)
            {
                double number = (double)value;
                bool hasMinimum = JsonReader.HasItem(property, "minimum");
                bool hasMaximum = JsonReader.HasItem(property, "maximum");
                double minimum = JsonReader.GetDouble(property, "minimum", double.MinValue);
                double maximum = JsonReader.GetDouble(property, "maximum", double.MaxValue);
                if (number >= minimum && number <= maximum) return null;
                //
                string range;
                if (hasMinimum && hasMaximum) range = "from " + FormatNumber(minimum) + " to " + FormatNumber(maximum);
                else if (hasMinimum) range = "at least " + FormatNumber(minimum);
                else range = "at most " + FormatNumber(maximum);
                return "must be " + range + ", not " + DescribeValue(value);
            }
            return null;
        }
        // Parsed JSON holds every number as a double, so a whole number is a double without a fraction
        private static bool IsOfType(object value, string type)
        {
            if (type == null) return true;
            if (value == null) return false;
            if (type == "boolean") return value is bool;
            if (type == "string") return value is string;
            if (type == "array") return value is List<object>;
            if (type == "object") return value is Dictionary<string, object>;
            if (type == "number") return value is double && !double.IsNaN((double)value) && !double.IsInfinity((double)value);
            if (type == "integer")
            {
                if (!(value is double)) return false;
                double number = (double)value;
                return number == Math.Floor(number) && number >= int.MinValue && number <= int.MaxValue;
            }
            return true;
        }
        private static string DescribeType(string type)
        {
            if (type == "boolean") return "true or false";
            if (type == "string") return "a string";
            if (type == "array") return "an array";
            if (type == "object") return "an object";
            if (type == "number") return "a number";
            if (type == "integer") return "a whole number";
            return type;
        }
        private static string DescribeValue(object value)
        {
            if (value == null) return "null";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is double) return FormatNumber((double)value);
            if (value is List<object>) return "an array";
            if (value is Dictionary<string, object>) return "an object";
            string text = value.ToString();
            if (text.Length > 40) text = text.Substring(0, 40) + "...";
            return "'" + text + "'";
        }
        private static string FormatNumber(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
