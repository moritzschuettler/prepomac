// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CaeGlobals
{
    // Compares two object graphs the way a file sees them: every serialized field, the items of the framework
    // collections and arrays, and whether the objects shared by the first graph are shared alike by the second
    public sealed class PmxGraphComparer
    {
        // Variables                                                                                                                
        private const BindingFlags DeclaredInstanceFields = BindingFlags.Instance | BindingFlags.Public |
                                                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<Type, FieldInfo[]> _fields = new Dictionary<Type, FieldInfo[]>();
        private readonly Dictionary<object, object> _pairs;
        private readonly List<string> _differences;
        private readonly int _maxDifferences;


        // Properties                                                                                                               
        public List<string> Differences { get { return _differences; } }


        // Constructors                                                                                                             
        public PmxGraphComparer(int maxDifferences = 20)
        {
            _pairs = new Dictionary<object, object>(PmxReferenceComparer.Instance);
            _differences = new List<string>();
            _maxDifferences = maxDifferences;
        }


        // Methods                                                                                                                  
        public static List<string> Compare(object first, object second, int maxDifferences = 20)
        {
            PmxGraphComparer comparer = new PmxGraphComparer(maxDifferences);
            comparer.CompareValues(first, second, "root");
            return comparer._differences;
        }
        private static FieldInfo[] GetFields(Type type)
        {
            FieldInfo[] fields;
            lock (_fields)
            {
                if (_fields.TryGetValue(type, out fields)) return fields;
            }
            List<FieldInfo> list = new List<FieldInfo>();
            for (Type level = type; level != null && level != typeof(object) && level != typeof(ValueType); level = level.BaseType)
            {
                foreach (var field in level.GetFields(DeclaredInstanceFields))
                {
                    if (field.IsNotSerialized || typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                    list.Add(field);
                }
            }
            fields = list.ToArray();
            lock (_fields) _fields[type] = fields;
            return fields;
        }
        private void AddDifference(string path, string text)
        {
            if (_differences.Count < _maxDifferences) _differences.Add(path + ": " + text);
        }
        private static string Describe(object value)
        {
            if (value == null) return "null";
            string text = value.ToString();
            if (text.Length > 80) text = text.Substring(0, 80) + "...";
            return value.GetType().Name + " " + text;
        }
        private void CompareValues(object first, object second, string path)
        {
            if (_differences.Count >= _maxDifferences) return;
            if (first == null || second == null)
            {
                if (first != null || second != null) AddDifference(path, Describe(first) + " != " + Describe(second));
                return;
            }
            if (first is Delegate) return;
            //
            Type type = first.GetType();
            if (type != second.GetType())
            {
                AddDifference(path, "type " + type.FullName + " != " + second.GetType().FullName);
                return;
            }
            if (type.IsPrimitive || type.IsEnum || first is string || first is decimal || first is DateTime ||
                first is TimeSpan || first is Guid || first is Type || (type.Namespace == "System.Drawing" && !type.IsArray))
            {
                if (!first.Equals(second)) AddDifference(path, Describe(first) + " != " + Describe(second));
                return;
            }
            if (first is System.IO.FileInfo fileInfo)
            {
                if (fileInfo.FullName != ((System.IO.FileInfo)second).FullName) AddDifference(path, "file info");
                return;
            }
            if (!type.IsValueType)
            {
                object pair;
                if (_pairs.TryGetValue(first, out pair))
                {
                    if (!ReferenceEquals(pair, second)) AddDifference(path, "shared reference not kept (" + type.Name + ")");
                    return;
                }
                if (ReferenceEquals(first, second))
                {
                    AddDifference(path, "the same object instead of a copy (" + type.Name + ")");
                    return;
                }
                _pairs.Add(first, second);
            }
            //
            if (first is Array firstArray)
            {
                Array secondArray = (Array)second;
                if (firstArray.Rank != secondArray.Rank || firstArray.Length != secondArray.Length)
                {
                    AddDifference(path, "array length " + firstArray.Length + " != " + secondArray.Length);
                    return;
                }
                IEnumerator a = firstArray.GetEnumerator();
                IEnumerator b = secondArray.GetEnumerator();
                int index = 0;
                while (a.MoveNext() && b.MoveNext()) CompareValues(a.Current, b.Current, path + "[" + index++ + "]");
                return;
            }
            if (!type.IsValueType && type.IsGenericType && type.Namespace == "System.Collections.Generic")
            {
                CompareCollection(first, second, path, type);
                return;
            }
            foreach (var field in GetFields(type))
                CompareValues(field.GetValue(first), field.GetValue(second), path + "." + GetFieldName(field));
        }
        private void CompareCollection(object first, object second, string path, Type type)
        {
            PropertyInfo comparerProperty = type.GetProperty("Comparer");
            if (comparerProperty != null)
            {
                object a = comparerProperty.GetValue(first);
                object b = comparerProperty.GetValue(second);
                if (a.GetType() != b.GetType() || (a is StringComparer && !a.Equals(b)))
                    AddDifference(path, "comparer " + a.GetType().Name + " != " + b.GetType().Name);
            }
            IEnumerator firstItems = ((IEnumerable)first).GetEnumerator();
            IEnumerator secondItems = ((IEnumerable)second).GetEnumerator();
            int index = 0;
            while (true)
            {
                bool hasFirst = firstItems.MoveNext();
                bool hasSecond = secondItems.MoveNext();
                if (hasFirst != hasSecond)
                {
                    AddDifference(path, "item count differs at " + index);
                    return;
                }
                if (!hasFirst) return;
                CompareValues(firstItems.Current, secondItems.Current, path + "[" + index++ + "]");
            }
        }
        private static string GetFieldName(FieldInfo field)
        {
            string name = field.Name;
            if (name.StartsWith("<")) name = name.Substring(1, name.IndexOf('>') - 1);
            return name;
        }
    }
}
