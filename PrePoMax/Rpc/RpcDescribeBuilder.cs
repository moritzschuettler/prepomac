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
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PrePoMax.Rpc
{
    // The API schema, read off the [PythonAPI] metadata that already documents every member. It is
    // the same source BaseAPI.help_text() reads, with a machine readable formatter instead of a
    // human readable one, so the schema an agent discovers cannot drift from the API it describes.
    //
    // Beside the hand written argument line, every method also reports its real parameters: the name,
    // whether it is optional, the default the signature carries, and - for a string argument the API
    // validates against an enumeration - the values it accepts. Those three come from the type
    // system and from PythonEnumVocabulary rather than from prose, so a caller does not have to
    // learn the vocabulary from a rejection.
    public static class RpcDescribeBuilder
    {
        // Static methods                                                                                                           
        public static string Build(string path, object target)
        {
            Type type = target == null ? null : target.GetType();
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("path", path);
            writer.WriteValue("resolvedAs", "path");
            writer.WriteValue("type", type == null ? "" : type.Name);
            writer.WriteValue("isApiObject", target is BaseAPI);
            WriteMembers(writer, type);
            WriteItemNames(writer, target);
            writer.EndObject();
            return writer.ToString();
        }
        // The same schema for a type that no live object is reachable from. A step holds the loads,
        // so pmx.model.steps['Step-1'].loads cannot be described before a step exists; the type can
        // be, and knowing what Loads offers is what decides whether the step is worth creating
        public static string BuildForType(string name, Type type)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("path", name);
            writer.WriteValue("resolvedAs", "type");
            writer.WriteValue("type", type.Name);
            writer.WriteValue("isApiObject", typeof(BaseAPI).IsAssignableFrom(type));
            writer.WriteValue("note", "This is the type, not an object of the model. It carries no item " +
                                      "names, because nothing was resolved against the open model.");
            WriteMembers(writer, type);
            writer.EndObject();
            return writer.ToString();
        }
        // A path that ends at a method or at a property holding a plain value is described by that member alone,
        // read off the type that owns it, because the object the path evaluates to is a bound method or a value
        // that carries none of the metadata. A property holding an API object is left to the type it returns,
        // whose members say more. Null when the type has no such member of that name
        public static string BuildForMember(string path, Type ownerType, string memberName)
        {
            List<MemberInfo> members = GetApiMembers(ownerType, memberName);
            members.RemoveAll(member => member is PropertyInfo &&
                                        typeof(BaseAPI).IsAssignableFrom(((PropertyInfo)member).PropertyType));
            if (members.Count == 0) return null;
            //
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.WriteValue("path", path);
            writer.WriteValue("resolvedAs", "member");
            writer.WriteValue("type", ownerType.Name);
            writer.WriteValue("isApiObject", false);
            writer.BeginArray("members");
            foreach (MemberInfo member in members) WriteMember(writer, ownerType, member);
            writer.EndArray();
            writer.EndObject();
            return writer.ToString();
        }
        // The names a path below an object of the type may continue with: its API members, and one level into
        // the API objects its properties hold, which is where a dot written as an underscore usually belongs
        public static List<string> GetReachableNames(Type type)
        {
            List<string> names = new List<string>();
            foreach (MemberInfo member in GetApiMembers(type, null))
            {
                if (!names.Contains(member.Name)) names.Add(member.Name);
                PropertyInfo property = member as PropertyInfo;
                if (property == null || !typeof(BaseAPI).IsAssignableFrom(property.PropertyType)) continue;
                //
                foreach (MemberInfo child in GetApiMembers(property.PropertyType, null))
                {
                    string name = property.Name + "." + child.Name;
                    if (!names.Contains(name)) names.Add(name);
                }
            }
            return names;
        }
        private static List<MemberInfo> GetApiMembers(Type type, string name)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            if (type == null) return members;
            //
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (name != null && property.Name != name) continue;
                if (GetAttribute(property) != null) members.Add(property);
            }
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName || (name != null && method.Name != name)) continue;
                if (GetAttribute(method) != null) members.Add(method);
            }
            return members;
        }
        private static void WriteMembers(JsonWriter writer, Type type)
        {
            writer.BeginArray("members");
            foreach (MemberInfo member in GetApiMembers(type, null)) WriteMember(writer, type, member);
            writer.EndArray();
        }
        private static void WriteMember(JsonWriter writer, Type type, MemberInfo member)
        {
            PythonAPIAttribute attribute = GetAttribute(member);
            PropertyInfo property = member as PropertyInfo;
            writer.BeginObject();
            writer.WriteValue("name", member.Name);
            if (property != null)
            {
                writer.WriteValue("kind", property.CanWrite ? "property" : "readOnlyProperty");
                writer.WriteValue("returns", GetReturns(attribute, property.PropertyType.Name));
                writer.WriteValue("description", attribute.Description);
                WriteValues(writer, "values",
                            PythonEnumVocabulary.GetValues(type.Name, property.Name, property.Name));
            }
            else
            {
                MethodInfo method = (MethodInfo)member;
                writer.WriteValue("kind", "method");
                writer.WriteValue("arguments", GetArguments(attribute, method));
                writer.WriteValue("returns", GetReturns(attribute, ""));
                writer.WriteValue("description", attribute.Description);
                WriteParameters(writer, type, method, attribute.Arguments);
            }
            WriteExample(writer, attribute);
            writer.EndObject();
        }
        private static void WriteParameters(JsonWriter writer, Type type, MethodInfo method, string arguments)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0) return;
            //
            writer.BeginArray("parameters");
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                writer.BeginObject();
                writer.WriteValue("name", parameter.Name);
                writer.WriteValue("isOptional", parameter.IsOptional);
                if (parameter.IsOptional) writer.WriteValue("default", GetDefault(parameter, arguments));
                WriteValues(writer, "values",
                            PythonEnumVocabulary.GetValues(type.Name, method.Name, parameter.Name));
                writer.EndObject();
            }
            writer.EndArray();
        }
        // A value the API accepts as a number, an expression, a list or a selection arrives as object,
        // so the declared parameter type says nothing a caller can use. The default does: it is what
        // the member does when the argument is left out, which on the model side means keeping what
        // is already there
        private static string GetDefault(ParameterInfo parameter, string arguments)
        {
            object value = parameter.RawDefaultValue;
            if (value == null || value == DBNull.Value) return GetDocumentedDefault(parameter.Name, arguments) ?? "None";
            if (value is bool) return (bool)value ? "True" : "False";
            if (value is string) return "'" + (string)value + "'";
            IFormattable formattable = value as IFormattable;
            if (formattable != null) return formattable.ToString(null, CultureInfo.InvariantCulture);
            //
            return value.ToString();
        }
        // A text argument with a default arrives as object, which C# gives no default but null, so the default the
        // member applies when the argument is left out is the one its Arguments text documents. Only the value
        // token is taken: free text after it, such as the range of a value left out as None, is not the default
        private static string GetDocumentedDefault(string parameterName, string arguments)
        {
            if (string.IsNullOrEmpty(arguments)) return null;
            //
            Match match = Regex.Match(arguments, @"(?:^|,)\s*" + Regex.Escape(parameterName) +
                                                 @"\s*:[^=,]*=\s*('[^']*'|""[^""]*""|\[[^\]]*\]|[^\s,(]+)");
            if (!match.Success) return null;
            //
            string value = match.Groups[1].Value.Trim();
            return value == "None" ? null : value;
        }
        private static void WriteValues(JsonWriter writer, string name, string[] values)
        {
            if (values == null || values.Length == 0) return;
            //
            writer.BeginArray(name);
            for (int i = 0; i < values.Length; i++) writer.WriteValue(values[i]);
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
