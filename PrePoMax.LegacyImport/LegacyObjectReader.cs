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
using System.Formats.Nrbf;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PrePoMax.LegacyImport
{
    // Reads a stream written by the binary formatter without the binary formatter: the records are decoded by
    // System.Formats.Nrbf and turned into objects the way the binary formatter did - an object is created without a
    // constructor and its fields are set by name, a type with a (SerializationInfo, StreamingContext) constructor is
    // given its members through that constructor, and the deserialization callbacks run once the graph is complete.
    public sealed class LegacyObjectReader
    {
        private sealed class PendingConstructor
        {
            public object Item;
            public ConstructorInfo Constructor;
            public SerializationInfo Info;
            public int Order;
            public int Completion;
            public bool CompletionResolved;
            public bool Resolving;
            public bool Started;
        }
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object item) { return RuntimeHelpers.GetHashCode(item); }
        }


        // Variables                                                                                                                
        private const BindingFlags DeclaredInstanceFields = BindingFlags.Instance | BindingFlags.Public |
                                                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Type[] _serializationConstructorTypes = new Type[] { typeof(SerializationInfo),
                                                                                     typeof(StreamingContext) };
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldsByType =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<Type, FieldInfo[]> _referenceFieldsByType = new Dictionary<Type, FieldInfo[]>();
        private readonly SerializationBinder _binder;
        private readonly Dictionary<SerializationRecordId, object> _objects;
        private readonly Dictionary<string, Type> _typeByName;
        private readonly List<object> _onDeserialized;
        private readonly List<IDeserializationCallback> _callbacks;
        private readonly Dictionary<object, PendingConstructor> _pendingConstructors;
        private readonly Dictionary<object, int> _reachedCompletion;
        private Dictionary<SerializationRecordId, int> _streamOrder;
        private readonly StreamingContext _context;
        private readonly FormatterConverter _converter;


        // Constructors                                                                                                             
        public LegacyObjectReader(SerializationBinder binder)
        {
            _binder = binder;
            _objects = new Dictionary<SerializationRecordId, object>();
            _typeByName = new Dictionary<string, Type>();
            _onDeserialized = new List<object>();
            _callbacks = new List<IDeserializationCallback>();
            _pendingConstructors = new Dictionary<object, PendingConstructor>(new ReferenceComparer());
            _reachedCompletion = new Dictionary<object, int>(new ReferenceComparer());
            _context = new StreamingContext(StreamingContextStates.All);
            _converter = new FormatterConverter();
        }


        // Methods                                                                                                                  
        public object Read(Stream stream)
        {
            PayloadOptions options = new PayloadOptions();
            options.UndoTruncatedTypeNames = true;
            IReadOnlyDictionary<SerializationRecordId, SerializationRecord> records;
            SerializationRecord root = NrbfDecoder.Decode(stream, out records, options, true);
            // The record map is filled as the records are read, so it lists them in the order of the stream
            _streamOrder = new Dictionary<SerializationRecordId, int>();
            foreach (var entry in records) _streamOrder[entry.Key] = _streamOrder.Count;
            object value = Materialize(root);
            InvokePendingConstructors();
            foreach (var item in _onDeserialized) InvokeCallbacks(item, typeof(OnDeserializedAttribute));
            foreach (var callback in _callbacks) callback.OnDeserialization(null);
            return value;
        }
        private object Materialize(object raw)
        {
            SerializationRecord record = raw as SerializationRecord;
            if (record == null) return raw;
            //
            if (record is PrimitiveTypeRecord primitive) return primitive.Value;
            if (record.RecordType == SerializationRecordType.ObjectNull ||
                record.RecordType == SerializationRecordType.ObjectNullMultiple ||
                record.RecordType == SerializationRecordType.ObjectNullMultiple256) return null;
            //
            object existing;
            if (_objects.TryGetValue(record.Id, out existing)) return existing;
            if (record is ClassRecord classRecord) return MaterializeClass(classRecord);
            if (record is ArrayRecord arrayRecord) return MaterializeArray(arrayRecord);
            throw new SerializationException("The record type " + record.RecordType + " is not supported.");
        }
        private Type ResolveType(System.Reflection.Metadata.TypeName typeName)
        {
            string fullName = typeName.FullName;
            string assemblyName = typeName.AssemblyName != null ? typeName.AssemblyName.FullName : "mscorlib";
            string key = fullName + ", " + assemblyName;
            Type type;
            if (_typeByName.TryGetValue(key, out type)) return type;
            //
            type = _binder != null ? _binder.BindToType(assemblyName, fullName) : null;
            if (type == null) type = Type.GetType(key, false);
            if (type == null && typeName.AssemblyName != null)
                type = Type.GetType(fullName + ", " + typeName.AssemblyName.Name, false);
            if (type == null) type = Type.GetType(fullName, false);
            _typeByName.Add(key, type);
            return type;
        }
        private object MaterializeClass(ClassRecord record)
        {
            string fullName = record.TypeName.FullName;
            if (fullName == "System.DelegateSerializationHolder") return Remember(record, null);
            if (fullName == "System.UnitySerializationHolder") return Remember(record, MaterializeUnity(record));
            //
            // As the binary formatter, a type that no longer exists is dropped as long as nothing needs it
            Type type = ResolveType(record.TypeName);
            if (type == null) return Remember(record, null);
            if (type.IsEnum) return Remember(record, Enum.ToObject(type, Materialize(record.GetRawValue("value__"))));
            //
            ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                              null, _serializationConstructorTypes, null);
            object item = FormatterServices.GetUninitializedObject(type);
            if (!type.IsValueType) Remember(record, item);
            // A type the binary formatter gave its members through this constructor still has it, while the domain types
            // no longer implement ISerializable, since nothing writes that format any more
            if (constructor != null)
            {
                SerializationInfo info = new SerializationInfo(type, _converter);
                int completion = GetStreamOrder(record);
                foreach (var name in record.MemberNames)
                {
                    object raw = record.GetRawValue(name);
                    if (raw is SerializationRecord member) completion = Math.Max(completion, GetStreamOrder(member));
                    object value = Materialize(raw);
                    info.AddValue(name, value, value != null ? value.GetType() : typeof(object));
                }
                // Some of these constructors read the objects they are given and some depend on static state set by others -
                // the unit system sets the units of the converters the materials use - so they run in the order the binary
                // formatter ran them
                if (type.IsValueType) constructor.Invoke(item, new object[] { info, _context });
                else
                {
                    _pendingConstructors.Add(item, new PendingConstructor { Item = item, Constructor = constructor, Info = info,
                                                                            Order = GetStreamOrder(record),
                                                                            Completion = completion });
                }
            }
            else
            {
                InvokeCallbacks(item, typeof(OnDeserializingAttribute));
                Dictionary<string, FieldInfo> fields = GetFields(type);
                FieldInfo field;
                foreach (var name in record.MemberNames)
                {
                    if (!fields.TryGetValue(name, out field)) continue;
                    field.SetValue(item, Convert(Materialize(record.GetRawValue(name)), field.FieldType));
                }
            }
            if (type.IsValueType) Remember(record, item);
            if (item is IDeserializationCallback callback) _callbacks.Add(callback);
            if (HasCallbacks(type, typeof(OnDeserializedAttribute))) _onDeserialized.Add(item);
            return item;
        }
        private void InvokePendingConstructors()
        {
            // The binary formatter ran such a constructor once the record of the object and the records of its members were
            // read and the constructors its members need had run - also those of objects held in a collection, which some
            // constructors read - and the member completed last went first
            List<PendingConstructor> pending = new List<PendingConstructor>(_pendingConstructors.Values);
            foreach (var constructor in pending) ResolveCompletion(constructor);
            pending.Sort((x, y) => x.Completion != y.Completion ? x.Completion.CompareTo(y.Completion) :
                                                                   y.Order.CompareTo(x.Order));
            foreach (var constructor in pending) InvokePendingConstructor(constructor);
        }
        private int ResolveCompletion(PendingConstructor pending)
        {
            if (pending.CompletionResolved || pending.Resolving) return pending.Completion;
            pending.Resolving = true;
            foreach (SerializationEntry entry in pending.Info)
                pending.Completion = Math.Max(pending.Completion, GetReachedCompletion(entry.Value));
            pending.Resolving = false;
            pending.CompletionResolved = true;
            return pending.Completion;
        }
        // The latest completion of the constructors reached from a value through the objects without one
        private int GetReachedCompletion(object value)
        {
            if (value == null || value is string || value is Type) return -1;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) return -1;
            PendingConstructor pending;
            if (_pendingConstructors.TryGetValue(value, out pending)) return ResolveCompletion(pending);
            int completion;
            if (!type.IsValueType)
            {
                if (_reachedCompletion.TryGetValue(value, out completion)) return completion;
                _reachedCompletion[value] = -1;
            }
            completion = -1;
            if (value is Array array)
            {
                Type elementType = type.GetElementType();
                if (!elementType.IsPrimitive && !elementType.IsEnum && elementType != typeof(string))
                {
                    foreach (var element in array) completion = Math.Max(completion, GetReachedCompletion(element));
                }
            }
            else
            {
                foreach (var field in GetReferenceFields(type))
                    completion = Math.Max(completion, GetReachedCompletion(field.GetValue(value)));
            }
            if (!type.IsValueType) _reachedCompletion[value] = completion;
            return completion;
        }
        private void InvokePendingConstructor(PendingConstructor pending)
        {
            if (pending.Started) return;
            pending.Started = true;
            pending.Constructor.Invoke(pending.Item, new object[] { pending.Info, _context });
        }
        private int GetStreamOrder(SerializationRecord record)
        {
            int order;
            if (_streamOrder.TryGetValue(record.Id, out order)) return order;
            return -1;
        }
        private object MaterializeUnity(ClassRecord record)
        {
            // A System.Type: the type name with its assembly
            int unityType = record.GetInt32("UnityType");
            if (unityType != 4) return null;
            string typeName = record.GetString("Data");
            string assemblyName = record.GetString("AssemblyName");
            Type type = _binder != null ? _binder.BindToType(assemblyName, typeName) : null;
            if (type == null) type = Type.GetType(typeName + ", " + assemblyName, false);
            if (type == null) type = Type.GetType(typeName + ", " + new AssemblyName(assemblyName).Name, false);
            return type;
        }
        private object MaterializeArray(ArrayRecord record)
        {
            Type arrayType = ResolveType(record.TypeName);
            if (arrayType == null) return Remember(record, null);
            Type elementType = arrayType.GetElementType();
            int[] lengths = new int[record.Rank];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = record.Lengths[i];
            //
            Array raw;
            if (elementType.IsPrimitive || elementType == typeof(string) || elementType == typeof(decimal) ||
                elementType == typeof(DateTime) || elementType == typeof(TimeSpan))
            {
                raw = record.GetArray(arrayType, true);
                return Remember(record, raw);
            }
            Array array = Array.CreateInstance(elementType, lengths);
            Remember(record, array);
            if (record is SZArrayRecord<SerializationRecord> objectArray) raw = objectArray.GetArray(true);
            else raw = record.GetArray(arrayType, true);
            int[] indices = new int[lengths.Length];
            foreach (var element in raw)
            {
                array.SetValue(Convert(Materialize(element), elementType), indices);
                for (int d = lengths.Length - 1; d >= 0; d--)
                {
                    if (++indices[d] < lengths[d]) break;
                    indices[d] = 0;
                }
            }
            return array;
        }
        private object Remember(SerializationRecord record, object value)
        {
            _objects[record.Id] = value;
            return value;
        }
        private object Convert(object value, Type type)
        {
            if (value == null || type.IsInstanceOfType(value)) return value;
            Type target = Nullable.GetUnderlyingType(type) ?? type;
            if (target.IsEnum) return Enum.ToObject(target, value);
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(target))
                return System.Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
            return value;
        }
        // The binary formatter names a field of the type itself by its name and a field of a base type by the name of
        // that type, a plus and the field name
        private static Dictionary<string, FieldInfo> GetFields(Type type)
        {
            Dictionary<string, FieldInfo> fields;
            lock (_fieldsByType)
            {
                if (_fieldsByType.TryGetValue(type, out fields)) return fields;
            }
            fields = new Dictionary<string, FieldInfo>();
            for (Type level = type; level != null && level != typeof(object); level = level.BaseType)
            {
                foreach (var field in level.GetFields(DeclaredInstanceFields))
                {
                    if (field.IsNotSerialized) continue;
                    string name = level == type ? field.Name : level.Name + "+" + field.Name;
                    if (!fields.ContainsKey(name)) fields.Add(name, field);
                    if (!fields.ContainsKey(field.Name)) fields.Add(field.Name, field);
                }
            }
            lock (_fieldsByType) _fieldsByType[type] = fields;
            return fields;
        }
        private static FieldInfo[] GetReferenceFields(Type type)
        {
            FieldInfo[] fields;
            lock (_referenceFieldsByType)
            {
                if (_referenceFieldsByType.TryGetValue(type, out fields)) return fields;
            }
            List<FieldInfo> list = new List<FieldInfo>();
            for (Type level = type; level != null && level != typeof(object); level = level.BaseType)
            {
                foreach (var field in level.GetFields(DeclaredInstanceFields))
                {
                    Type fieldType = field.FieldType;
                    if (fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType == typeof(string)) continue;
                    list.Add(field);
                }
            }
            fields = list.ToArray();
            lock (_referenceFieldsByType) _referenceFieldsByType[type] = fields;
            return fields;
        }
        private static bool HasCallbacks(Type type, Type attribute)
        {
            for (Type level = type; level != null && level != typeof(object); level = level.BaseType)
            {
                foreach (var method in level.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.DeclaredOnly))
                {
                    if (method.IsDefined(attribute, false)) return true;
                }
            }
            return false;
        }
        private void InvokeCallbacks(object item, Type attribute)
        {
            List<Type> hierarchy = new List<Type>();
            for (Type level = item.GetType(); level != null && level != typeof(object); level = level.BaseType)
                hierarchy.Insert(0, level);
            foreach (var level in hierarchy)
            {
                foreach (var method in level.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.DeclaredOnly))
                {
                    if (method.IsDefined(attribute, false)) method.Invoke(item, new object[] { _context });
                }
            }
        }
    }
}
