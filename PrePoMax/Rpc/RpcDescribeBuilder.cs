// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace PrePoMax.Rpc
{
    // The API schema, read off the [PythonAPI] metadata that already documents every member. It is
    // the same source BaseAPI.help_text() reads, with a machine readable formatter instead of a
    // human readable one, so the schema an agent discovers cannot drift from the API it describes.
    public static class RpcDescribeBuilder
    {
        // Static methods                                                                                                           
        public static string Build(string path, object target)
        {
            Type type = target == null ? null : target.GetType();
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("path", path);
            writer.WriteValue("type", type == null ? "" : type.Name);
            writer.WriteValue("isApiObject", target is BaseAPI);
            WriteMembers(writer, type);
            WriteItemNames(writer, target);
            writer.EndObject();
            return writer.ToString();
        }
        private static void WriteMembers(JsonWriter writer, Type type)
        {
            writer.BeginArray("members");
            if (type == null)
            {
                writer.EndArray();
                return;
            }
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                PythonAPIAttribute attribute = GetAttribute(property);
                if (attribute == null) continue;
                //
                writer.BeginObject();
                writer.WriteValue("name", property.Name);
                writer.WriteValue("kind", property.CanWrite ? "property" : "readOnlyProperty");
                writer.WriteValue("returns", GetReturns(attribute, property.PropertyType.Name));
                writer.WriteValue("description", attribute.Description);
                WriteExample(writer, attribute);
                writer.EndObject();
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName) continue;
                PythonAPIAttribute attribute = GetAttribute(method);
                if (attribute == null) continue;
                //
                writer.BeginObject();
                writer.WriteValue("name", method.Name);
                writer.WriteValue("kind", "method");
                writer.WriteValue("arguments", GetArguments(attribute, method));
                writer.WriteValue("returns", GetReturns(attribute, ""));
                writer.WriteValue("description", attribute.Description);
                WriteExample(writer, attribute);
                writer.EndObject();
            }
            writer.EndArray();
        }
        // A collection says what is in it right now, which is what an agent needs before it asks for
        // an item by name
        private static void WriteItemNames(JsonWriter writer, object target)
        {
            IEnumerable<string> names = GetItemNames(target);
            if (names == null) return;
            //
            writer.BeginArray("names");
            foreach (string name in names) writer.WriteValue(name);
            writer.EndArray();
        }
        private static IEnumerable<string> GetItemNames(object target)
        {
            if (target == null) return null;
            MethodInfo method = target.GetType().GetMethod("get_names", BindingFlags.Instance | BindingFlags.Public,
                                                           null, Type.EmptyTypes, null);
            if (method == null) return null;
            //
            try
            {
                object result = method.Invoke(target, null);
                IEnumerable items = result as IEnumerable;
                if (items == null) return null;
                //
                List<string> names = new List<string>();
                foreach (object item in items) names.Add(item == null ? "" : item.ToString());
                return names;
            }
            catch (Exception)
            {
                // A collection that cannot be read right now is described without its content
                return null;
            }
        }
        private static PythonAPIAttribute GetAttribute(MemberInfo member)
        {
            object[] attributes = member.GetCustomAttributes(typeof(PythonAPIAttribute), true);
            if (attributes == null || attributes.Length == 0) return null;
            return (PythonAPIAttribute)attributes[0];
        }
        private static string GetArguments(PythonAPIAttribute attribute, MethodInfo method)
        {
            if (!string.IsNullOrEmpty(attribute.Arguments)) return attribute.Arguments;
            //
            ParameterInfo[] parameters = method.GetParameters();
            List<string> names = new List<string>();
            for (int i = 0; i < parameters.Length; i++) names.Add(parameters[i].Name);
            return string.Join(", ", names.ToArray());
        }
        private static string GetReturns(PythonAPIAttribute attribute, string fallback)
        {
            return string.IsNullOrEmpty(attribute.Returns) ? fallback : attribute.Returns;
        }
        private static void WriteExample(JsonWriter writer, PythonAPIAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.Example)) return;
            writer.WriteValue("example", attribute.Example);
        }
    }
}
