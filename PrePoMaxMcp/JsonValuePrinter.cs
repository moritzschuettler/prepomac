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

namespace PrePoMaxMcp
{
    // Renders a value that came back from PrePoMax as JSON again, so a tool result can be handed to
    // an agent as it arrived. JsonWriter does the escaping and the indentation; this walks the tree
    // JsonReader produced.
    public static class JsonValuePrinter
    {
        // Static methods                                                                                                           
        public static string Write(object value)
        {
            JsonWriter writer = new JsonWriter();
            WriteValue(writer, null, value);
            return writer.ToString();
        }
        private static void WriteValue(JsonWriter writer, string name, object value)
        {
            Dictionary<string, object> items = value as Dictionary<string, object>;
            if (items != null)
            {
                if (name == null) writer.BeginObject();
                else writer.BeginObject(name);
                foreach (KeyValuePair<string, object> item in items) WriteValue(writer, item.Key, item.Value);
                writer.EndObject();
                return;
            }
            List<object> values = value as List<object>;
            if (values != null)
            {
                if (name == null) writer.BeginArray();
                else writer.BeginArray(name);
                foreach (object item in values) WriteValue(writer, null, item);
                writer.EndArray();
                return;
            }
            if (name == null) writer.WriteRawValue(GetScalar(value));
            else writer.WriteRawValue(name, GetScalar(value));
        }
        // The scalar is rendered rather than written by name, so that an array member keeps its type:
        // a bool stays a bool and a number stays a number
        private static string GetScalar(object value)
        {
            if (value == null) return "null";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is double) return WriteNumber((double)value);
            //
            JsonWriter writer = new JsonWriter();
            writer.WriteValue(value.ToString());
            return writer.ToString();
        }
        private static string WriteNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "null";
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
