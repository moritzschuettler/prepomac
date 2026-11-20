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
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    // Writes the stored fields of [PmxType] objects as JSON a person can read and edit: a field by its name without the
    // leading underscore, an enumeration by its member name, a color by its name or as #AARRGGBB. Reading fills an
    // object that already holds its defaults, so a field the file does not name keeps its default and a name the
    // object no longer has is ignored. A value whose type differs from the declared type of its field names its type
    // number in "$type". Meant for small trees such as the settings; shared references are written as copies.
    public static class PmxJsonSerializer
    {
        // Variables                                                                                                                
        private const string TypeProperty = "$type";
        private const int MaxDepth = 64;


        // Methods                                                                                                                  
        public static string Serialize(object value)
        {
            JsonWriter writer = new JsonWriter();
            WriteValue(writer, null, value, typeof(object), 0);
            return writer.ToString();
        }
        public static void Populate(object target, string json)
        {
            Dictionary<string, object> node = JsonReader.Parse(json) as Dictionary<string, object>;
            if (node == null) throw new PmxStorageException("The JSON text does not hold an object.");
            PopulateObject(target, node, 0);
        }
        public static void Populate(object target, Dictionary<string, object> node)
        {
            PopulateObject(target, node, 0);
        }
        public static object Deserialize(string json, Type type)
        {
            return ReadValue(JsonReader.Parse(json), type, null, 0);
        }
        // The stored fields of a type and of its bases, by the names they carry in a file
        private static List<KeyValuePair<string, FieldInfo>> GetFields(Type type)
        {
            List<KeyValuePair<string, FieldInfo>> fields = new List<KeyValuePair<string, FieldInfo>>();
            List<Type> hierarchy = new List<Type>();
            for (Type level = type; level != null && level != typeof(object) && level != typeof(ValueType); level = level.BaseType)
                hierarchy.Insert(0, level);
            int levelId;
            foreach (var level in hierarchy)
            {
                if (!PmxClassPlan.IsStoredType(level)) continue;
                foreach (var entry in PmxClassPlan.GetStoredFields(level, out levelId))
                    fields.Add(new KeyValuePair<string, FieldInfo>(GetName(entry.Key), entry.Key));
            }
            return fields;
        }
        private static string GetName(FieldInfo field)
        {
            string name = field.Name;
            if (name.StartsWith("<")) return name.Substring(1, name.IndexOf('>') - 1);
            return name.TrimStart('_');
        }
        private static void WriteValue(JsonWriter writer, string name, object value, Type declaredType, int depth)
        {
            if (depth > MaxDepth) throw new PmxStorageException("The object tree is too deep or has a cycle.");
            if (value == null || value is Delegate)
            {
                WriteRaw(writer, name, "null");
                return;
            }
            Type type = value.GetType();
            if (value is string text) WriteString(writer, name, text);
            else if (value is bool flag) WriteRaw(writer, name, flag ? "true" : "false");
            else if (type.IsEnum) WriteString(writer, name, value.ToString());
            else if (value is double || value is float)
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number))
                    WriteString(writer, name, number.ToString(CultureInfo.InvariantCulture));
                else WriteRaw(writer, name, number.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (type.IsPrimitive || value is decimal)
                WriteRaw(writer, name, Convert.ToString(value, CultureInfo.InvariantCulture));
            else if (value is DateTime dateTime) WriteString(writer, name, dateTime.ToString("o", CultureInfo.InvariantCulture));
            else if (value is TimeSpan timeSpan) WriteString(writer, name, timeSpan.ToString("c", CultureInfo.InvariantCulture));
            else if (value is Guid guid) WriteString(writer, name, guid.ToString());
            else if (value is Color color) WriteString(writer, name, ColorToText(color));
            else if (value is Type typeValue) WriteString(writer, name, typeValue.AssemblyQualifiedName);
            else if (value is Size size) WriteNumbers(writer, name, size.Width, size.Height);
            else if (value is Point point) WriteNumbers(writer, name, point.X, point.Y);
            else if (value is IDictionary dictionary && IsStringKeyed(type))
            {
                BeginObject(writer, name);
                Type valueType = GetDictionaryValueType(type);
                foreach (DictionaryEntry entry in dictionary)
                    WriteValue(writer, (string)entry.Key, entry.Value, valueType, depth + 1);
                writer.EndObject();
            }
            else if (value is IDictionary otherDictionary)
            {
                BeginArray(writer, name);
                Type[] arguments = type.GetGenericArguments();
                foreach (DictionaryEntry entry in otherDictionary)
                {
                    writer.BeginArray();
                    WriteValue(writer, null, entry.Key, arguments[0], depth + 1);
                    WriteValue(writer, null, entry.Value, arguments[1], depth + 1);
                    writer.EndArray();
                }
                writer.EndArray();
            }
            else if (value is IEnumerable items && (type.IsArray || type.Namespace == "System.Collections.Generic"))
            {
                BeginArray(writer, name);
                Type itemType = GetItemType(type);
                foreach (var item in items) WriteValue(writer, null, item, itemType, depth + 1);
                writer.EndArray();
            }
            else if (PmxClassPlan.IsStoredType(type))
            {
                BeginObject(writer, name);
                int id;
                if (type != declaredType && PmxTypeRegistry.TryGetId(type, out id)) WriteRaw(writer, TypeProperty, id.ToString());
                foreach (var entry in GetFields(type))
                    WriteValue(writer, entry.Key, entry.Value.GetValue(value), entry.Value.FieldType, depth + 1);
                writer.EndObject();
            }
            else throw new PmxStorageException("The type " + type.FullName + " cannot be written as JSON.");
        }
        private static void WriteRaw(JsonWriter writer, string name, string json)
        {
            if (name == null) writer.WriteRawValue(json);
            else writer.WriteRawValue(name, json);
        }
        private static void WriteString(JsonWriter writer, string name, string text)
        {
            if (name == null) writer.WriteValue(text);
            else writer.WriteValue(name, text);
        }
        private static void WriteNumbers(JsonWriter writer, string name, params int[] numbers)
        {
            BeginArray(writer, name);
            foreach (var number in numbers) writer.WriteRawValue(number.ToString(CultureInfo.InvariantCulture));
            writer.EndArray();
        }
        private static void BeginObject(JsonWriter writer, string name)
        {
            if (name == null) writer.BeginObject();
            else writer.BeginObject(name);
        }
        private static void BeginArray(JsonWriter writer, string name)
        {
            if (name == null) writer.BeginArray();
            else writer.BeginArray(name);
        }
        private static string ColorToText(Color color)
        {
            if (color.IsEmpty) return "";
            if (color.IsKnownColor) return color.Name;
            return "#" + color.ToArgb().ToString("X8", CultureInfo.InvariantCulture);
        }
        private static Color TextToColor(string text)
        {
            if (string.IsNullOrEmpty(text)) return Color.Empty;
            if (text.StartsWith("#"))
                return Color.FromArgb(int.Parse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            return Color.FromName(text);
        }
        private static bool IsStringKeyed(Type type)
        {
            return type.IsGenericType && type.GetGenericArguments()[0] == typeof(string);
        }
        private static Type GetDictionaryValueType(Type type)
        {
            return type.IsGenericType ? type.GetGenericArguments()[1] : typeof(object);
        }
        private static Type GetItemType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType) return type.GetGenericArguments()[0];
            return typeof(object);
        }
        private static void PopulateObject(object target, Dictionary<string, object> node, int depth)
        {
            if (depth > MaxDepth) throw new PmxStorageException("The JSON text is too deep.");
            object value;
            foreach (var entry in GetFields(target.GetType()))
            {
                if (!node.TryGetValue(entry.Key, out value)) continue;
                entry.Value.SetValue(target, ReadValue(value, entry.Value.FieldType, entry.Value.GetValue(target), depth + 1));
            }
            // A type checks the values it was given as it does after the binary formatter read it
            StreamingContext context = new StreamingContext(StreamingContextStates.All);
            for (Type level = target.GetType(); level != null && level != typeof(object); level = level.BaseType)
            {
                foreach (var method in level.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.DeclaredOnly))
                {
                    if (method.IsDefined(typeof(OnDeserializedAttribute), false)) method.Invoke(target, new object[] { context });
                }
            }
        }
        private static object ReadValue(object node, Type type, object current, int depth)
        {
            if (node == null) return type.IsValueType ? Activator.CreateInstance(type) : null;
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) type = nullable;
            //
            Dictionary<string, object> objectNode = node as Dictionary<string, object>;
            if (objectNode != null && objectNode.ContainsKey(TypeProperty))
                type = PmxTypeRegistry.GetType(Convert.ToInt32(objectNode[TypeProperty], CultureInfo.InvariantCulture));
            //
            if (type == typeof(string)) return Convert.ToString(node, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return Convert.ToBoolean(node, CultureInfo.InvariantCulture);
            if (type.IsEnum)
            {
                if (node is string enumText) return Enum.Parse(type, enumText, true);
                return Enum.ToObject(type, Convert.ToInt64(node, CultureInfo.InvariantCulture));
            }
            if (type == typeof(double) || type == typeof(float))
            {
                double number = node is string numberText ? double.Parse(numberText, CultureInfo.InvariantCulture) :
                                Convert.ToDouble(node, CultureInfo.InvariantCulture);
                return type == typeof(float) ? (object)(float)number : number;
            }
            if (type.IsPrimitive || type == typeof(decimal)) return Convert.ChangeType(node, type, CultureInfo.InvariantCulture);
            if (type == typeof(DateTime))
                return DateTime.Parse((string)node, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (type == typeof(TimeSpan)) return TimeSpan.Parse((string)node, CultureInfo.InvariantCulture);
            if (type == typeof(Guid)) return new Guid((string)node);
            if (type == typeof(Color)) return TextToColor((string)node);
            if (typeof(Type).IsAssignableFrom(type)) return Type.GetType((string)node, false);
            if (type == typeof(Size) || type == typeof(Point))
            {
                List<object> numbers = (List<object>)node;
                int a = Convert.ToInt32(numbers[0], CultureInfo.InvariantCulture);
                int b = Convert.ToInt32(numbers[1], CultureInfo.InvariantCulture);
                return type == typeof(Size) ? (object)new Size(a, b) : new Point(a, b);
            }
            if (type.IsArray)
            {
                List<object> list = (List<object>)node;
                Type itemType = type.GetElementType();
                Array array = Array.CreateInstance(itemType, list.Count);
                for (int i = 0; i < list.Count; i++) array.SetValue(ReadValue(list[i], itemType, null, depth + 1), i);
                return array;
            }
            if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
            {
                IDictionary dictionary = (IDictionary)CreateCollection(type, current);
                Type[] arguments = type.GetGenericArguments();
                dictionary.Clear();
                if (objectNode != null)
                {
                    foreach (var entry in objectNode)
                        dictionary[entry.Key] = ReadValue(entry.Value, arguments[1], null, depth + 1);
                }
                else
                {
                    foreach (List<object> pair in (List<object>)node)
                        dictionary[ReadValue(pair[0], arguments[0], null, depth + 1)] =
                            ReadValue(pair[1], arguments[1], null, depth + 1);
                }
                return dictionary;
            }
            if (type.IsGenericType && type.Namespace == "System.Collections.Generic")
            {
                object collection = CreateCollection(type, current);
                Type itemType = type.GetGenericArguments()[0];
                MethodInfo clear = type.GetMethod("Clear");
                Type[] parameterTypes = new Type[] { itemType };
                MethodInfo add = type.GetMethod("Add", parameterTypes) ?? type.GetMethod("AddLast", parameterTypes) ??
                                 type.GetMethod("Enqueue", parameterTypes);
                clear.Invoke(collection, null);
                foreach (var item in (List<object>)node)
                    add.Invoke(collection, new object[] { ReadValue(item, itemType, null, depth + 1) });
                return collection;
            }
            if (PmxClassPlan.IsStoredType(type) && objectNode != null)
            {
                object item = current != null && current.GetType() == type ? current : CreateObject(type);
                PopulateObject(item, objectNode, depth + 1);
                return item;
            }
            throw new PmxStorageException("The type " + type.FullName + " cannot be read from JSON.");
        }
        private static object CreateCollection(Type type, object current)
        {
            if (current != null && current.GetType() == type) return current;
            return Activator.CreateInstance(type);
        }
        // An object starts from what its parameterless constructor sets, so that a field the file leaves out has its
        // default; a type without one starts empty
        private static object CreateObject(Type type)
        {
            ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                              null, Type.EmptyTypes, null);
            if (constructor != null) return constructor.Invoke(null);
            return FormatterServices.GetUninitializedObject(type);
        }
    }
}
