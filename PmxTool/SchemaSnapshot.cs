// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using CaeGlobals;

namespace PmxTool
{
    // The stored schema as text, one entry per line:
    //   T <type number> <kind> <type name>
    //   F <type number>.<key> <field type> <field name>
    //   E <type number>.<value> member <member name>   a member of a stored enumeration
    //   R <entry>                             a retired entry, whose number or key may never be used again
    // A field type names a stored type by its number (#2001), so renaming a type or a field changes no entry; only the
    // names after the meaning are informational. An entry whose meaning changed, or that disappeared, fails the check.
    public static class SchemaSnapshot
    {
        // Methods                                                                                                                  
        public static List<string> Create()
        {
            List<string> lines = new List<string>();
            int levelId;
            foreach (var entry in PmxTypeRegistry.GetAllTypes())
            {
                Type type = entry.Value;
                lines.Add("T " + entry.Key + " " + GetKind(type) + " " + type.FullName);
                // An enumeration is stored by number, so each number of a member is an entry
                if (type.IsEnum)
                {
                    SortedDictionary<long, string> members = new SortedDictionary<long, string>();
                    foreach (var name in Enum.GetNames(type))
                    {
                        long value = Convert.ToInt64(Enum.Parse(type, name));
                        if (!members.ContainsKey(value)) members.Add(value, name);
                    }
                    foreach (var member in members) lines.Add("E " + entry.Key + "." + member.Key + " member " + member.Value);
                    continue;
                }
                //
                List<KeyValuePair<FieldInfo, int>> fields = PmxClassPlan.GetStoredFields(type, out levelId);
                fields.Sort((a, b) => a.Value.CompareTo(b.Value));
                foreach (var field in fields)
                {
                    lines.Add("F " + entry.Key + "." + field.Value + " " + Describe(field.Key.FieldType) + " " +
                              GetFieldName(field.Key));
                }
            }
            return lines;
        }
        private static string GetKind(Type type)
        {
            if (type.IsEnum) return "enum";
            if (type.IsValueType) return "struct";
            if (type.IsGenericTypeDefinition) return "class`" + type.GetGenericArguments().Length;
            return "class";
        }
        private static string GetFieldName(FieldInfo field)
        {
            string name = field.Name;
            if (name.StartsWith("<")) name = name.Substring(1, name.IndexOf('>') - 1);
            return name;
        }
        private static string Describe(Type type)
        {
            int id;
            if (type.IsGenericParameter) return "$" + type.GenericParameterPosition;
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if (type == elementType.MakeArrayType()) return Describe(elementType) + "[]";
                return Describe(elementType) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                return Describe(type.GetGenericTypeDefinition()) + "<" +
                       string.Join(",", type.GetGenericArguments().Select(Describe)) + ">";
            }
            if (PmxTypeRegistry.TryGetId(type, out id)) return "#" + id;
            return type.FullName;
        }
        private static Dictionary<string, KeyValuePair<string, bool>> Parse(IEnumerable<string> lines)
        {
            // The key of an entry is its kind and number, its meaning the text that follows up to the name
            Dictionary<string, KeyValuePair<string, bool>> entries = new Dictionary<string, KeyValuePair<string, bool>>();
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                bool retired = line.StartsWith("R ");
                if (retired) line = line.Substring(2);
                string[] parts = line.Split(' ');
                if (parts.Length < 3) continue;
                entries[parts[0] + " " + parts[1]] = new KeyValuePair<string, bool>(parts[2], retired);
            }
            return entries;
        }
        // The check and changed callbacks of an equation container are not stored, so an owner that wires them in its
        // setters - SetX(EquationContainer value, bool checkEquation) - wires them again after every read, a clone
        // included, in an [OnDeserialized] method that calls those setters
        private static List<string> GetEquationOwnersWithoutCallbacks()
        {
            List<string> errors = new List<string>();
            BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var entry in PmxTypeRegistry.GetAllTypes())
            {
                for (Type level = entry.Value; level != null && level != typeof(object); level = level.BaseType)
                {
                    if (level.IsEnum || level.IsValueType) break;
                    bool wiresEquations = level.GetMethods(declared).Any(method => IsEquationSetter(method));
                    if (!wiresEquations) continue;
                    bool hasCallback = level.GetMethods(declared).Any(method => method.IsDefined(typeof(OnDeserializedAttribute), false));
                    string error = level.FullName + " stores equation containers but has no [OnDeserialized] method that wires " +
                                   "their callbacks again (see RestoreEquationCallbacks in CaeModel.STLoad)";
                    if (!hasCallback && !errors.Contains(error)) errors.Add(error);
                }
            }
            return errors;
        }
        private static bool IsEquationSetter(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2 && parameters[1].ParameterType == typeof(bool) &&
                   (parameters[0].ParameterType == typeof(EquationContainer) ||
                    parameters[0].ParameterType == typeof(EquationContainer[][]));
        }
        public static int Run(string snapshotFileName, bool update)
        {
            List<string> current = Create();
            Dictionary<string, KeyValuePair<string, bool>> now = Parse(current);
            List<string> oldLines = new List<string>();
            if (File.Exists(snapshotFileName)) oldLines.AddRange(File.ReadAllLines(snapshotFileName));
            Dictionary<string, KeyValuePair<string, bool>> old = Parse(oldLines);
            //
            List<string> errors = new List<string>();
            List<string> retired = new List<string>();
            KeyValuePair<string, bool> entry;
            foreach (var oldEntry in old)
            {
                bool exists = now.TryGetValue(oldEntry.Key, out entry);
                if (oldEntry.Value.Value)
                {
                    if (exists && entry.Key != oldEntry.Value.Key)
                        errors.Add(oldEntry.Key + " reuses a retired number as " + entry.Key + " (was " + oldEntry.Value.Key + ")");
                    if (!exists) retired.Add(oldEntry.Key);
                }
                else if (!exists)
                {
                    if (update) retired.Add(oldEntry.Key);
                    else errors.Add(oldEntry.Key + " " + oldEntry.Value.Key + " was removed; run PmxTool schema --update to " +
                                    "retire it, so that its number is never used again");
                }
                else if (entry.Key != oldEntry.Value.Key)
                    errors.Add(oldEntry.Key + " changed its meaning from " + oldEntry.Value.Key + " to " + entry.Key +
                               "; give the changed type or field a new number instead");
            }
            int newEntries = now.Keys.Count(key => !old.ContainsKey(key));
            errors.AddRange(GetEquationOwnersWithoutCallbacks());
            //
            if (errors.Count > 0)
            {
                foreach (var error in errors) Console.Error.WriteLine("Schema snapshot error: " + error);
                return 1;
            }
            if (update)
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine("# The stored schema of PrePoMax files, checked by PmxTool schema --check after every build.");
                text.AppendLine("# A number in this file is never reused; see PmxTool/SchemaSnapshot.cs.");
                foreach (var line in current) text.AppendLine(line);
                Dictionary<string, string> oldLineByKey = new Dictionary<string, string>();
                foreach (var line in oldLines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                    string withoutRetired = trimmed.StartsWith("R ") ? trimmed.Substring(2) : trimmed;
                    string[] parts = withoutRetired.Split(' ');
                    oldLineByKey[parts[0] + " " + parts[1]] = withoutRetired;
                }
                foreach (var key in retired) text.AppendLine("R " + oldLineByKey[key]);
                File.WriteAllText(snapshotFileName, text.ToString());
                Console.WriteLine("Schema snapshot updated: " + now.Count + " entries, " + newEntries + " new, " +
                                  retired.Count + " retired.");
            }
            else if (newEntries > 0)
                Console.WriteLine("Schema snapshot: " + newEntries + " new entries are not in the snapshot yet; run " +
                                  "PmxTool schema --update to add them.");
            return 0;
        }
    }
}
