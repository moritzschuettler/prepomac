// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using MessagePack;

namespace CaeGlobals
{
    // How one [PmxType] type is stored: its fields of every class of its hierarchy, each under the key of the class that
    // declares it combined with the field key, so that a class and its base number their fields independently
    public sealed class PmxClassPlan
    {
        // Variables                                                                                                                
        private const BindingFlags DeclaredInstanceFields = BindingFlags.Instance | BindingFlags.Public |
                                                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly ConcurrentDictionary<Type, PmxClassPlan> _plans = new ConcurrentDictionary<Type, PmxClassPlan>();
        //
        private readonly Type _type;
        private readonly byte[] _typeReference;
        private readonly PmxMember[] _members;
        private readonly Dictionary<int, PmxMember> _memberByKey;
        private readonly MethodInfo[] _onSerializing;
        private readonly MethodInfo[] _onSerialized;
        private readonly MethodInfo[] _onDeserializing;
        private readonly MethodInfo[] _onDeserialized;
        private readonly bool _isDeserializationCallback;


        // Properties                                                                                                               
        public Type Type { get { return _type; } }
        public PmxMember[] Members { get { return _members; } }
        public bool HasDeserializedCallbacks { get { return _onDeserialized.Length > 0 || _isDeserializationCallback; } }


        // Constructors                                                                                                             
        private PmxClassPlan(Type type)
        {
            _type = type;
            _typeReference = PmxTypeCodec.GetTypeReferenceBytes(type);
            //
            List<Type> hierarchy = new List<Type>();
            for (Type level = type; level != null && level != typeof(object) && level != typeof(ValueType); level = level.BaseType)
                hierarchy.Insert(0, level);
            //
            List<PmxMember> members = new List<PmxMember>();
            List<MethodInfo> onSerializing = new List<MethodInfo>();
            List<MethodInfo> onSerialized = new List<MethodInfo>();
            List<MethodInfo> onDeserializing = new List<MethodInfo>();
            List<MethodInfo> onDeserialized = new List<MethodInfo>();
            _memberByKey = new Dictionary<int, PmxMember>();
            foreach (var level in hierarchy)
            {
                AddMembers(level, members);
                AddCallbacks(level, onSerializing, onSerialized, onDeserializing, onDeserialized);
            }
            _members = members.ToArray();
            foreach (var member in _members) _memberByKey.Add(member.CombinedKey, member);
            _onSerializing = onSerializing.ToArray();
            _onSerialized = onSerialized.ToArray();
            _onDeserializing = onDeserializing.ToArray();
            _onDeserialized = onDeserialized.ToArray();
            _isDeserializationCallback = typeof(IDeserializationCallback).IsAssignableFrom(type);
        }


        // Static methods                                                                                                           
        public static bool IsStoredType(Type type)
        {
            Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            return definition.IsDefined(typeof(PmxTypeAttribute), false);
        }
        public static PmxClassPlan Get(Type type)
        {
            PmxClassPlan plan;
            if (_plans.TryGetValue(type, out plan)) return plan;
            if (!IsStoredType(type)) throw new PmxStorageException("The type " + type.FullName + " has no storage schema.");
            plan = new PmxClassPlan(type);
            return _plans.GetOrAdd(type, plan);
        }
        public static PmxClassPlan TryGet(Type type)
        {
            PmxClassPlan plan;
            if (_plans.TryGetValue(type, out plan)) return plan;
            if (!IsStoredType(type)) return null;
            return Get(type);
        }
        // The fields a level stores; a field must carry a key unless it is not serialized or holds a delegate, which is
        // never stored
        public static List<KeyValuePair<FieldInfo, int>> GetStoredFields(Type level, out int levelId)
        {
            List<KeyValuePair<FieldInfo, int>> fields = new List<KeyValuePair<FieldInfo, int>>();
            levelId = -1;
            bool isStored = IsStoredType(level);
            if (isStored && !PmxTypeRegistry.TryGetId(level.IsGenericType ? level.GetGenericTypeDefinition() : level,
                                                      out levelId))
                throw new PmxStorageException("The stored type " + level.FullName + " is not registered.");
            //
            PmxKeyAttribute key;
            HashSet<int> keys = new HashSet<int>();
            foreach (var field in level.GetFields(DeclaredInstanceFields))
            {
                if (field.IsNotSerialized) continue;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                //
                if (!isStored)
                    throw new PmxStorageException("The type " + level.FullName + " is a base of a stored type and holds " +
                                                  "the field " + field.Name + ", but it has no storage schema.");
                key = field.GetCustomAttribute<PmxKeyAttribute>(false);
                if (key == null)
                    throw new PmxStorageException("The field " + field.Name + " of the stored type " + level.FullName +
                                                  " has no storage key. Mark it [PmxKey(n)] or [NonSerialized].");
                if (!keys.Add(key.Key))
                    throw new PmxStorageException("The storage key " + key.Key + " is used twice in " + level.FullName + ".");
                fields.Add(new KeyValuePair<FieldInfo, int>(field, key.Key));
            }
            return fields;
        }
        private static void AddMembers(Type level, List<PmxMember> members)
        {
            int levelId;
            foreach (var entry in GetStoredFields(level, out levelId))
                members.Add(PmxMember.Create(entry.Key, (levelId << 10) | entry.Value));
        }
        private static void AddCallbacks(Type level, List<MethodInfo> onSerializing, List<MethodInfo> onSerialized,
                                         List<MethodInfo> onDeserializing, List<MethodInfo> onDeserialized)
        {
            foreach (var method in level.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.DeclaredOnly))
            {
                if (method.IsDefined(typeof(OnSerializingAttribute), false)) onSerializing.Add(method);
                if (method.IsDefined(typeof(OnSerializedAttribute), false)) onSerialized.Add(method);
                if (method.IsDefined(typeof(OnDeserializingAttribute), false)) onDeserializing.Add(method);
                if (method.IsDefined(typeof(OnDeserializedAttribute), false)) onDeserialized.Add(method);
            }
        }


        // Methods                                                                                                                  
        public object CreateUninitialized()
        {
            return FormatterServices.GetUninitializedObject(_type);
        }
        public void WriteTypeReference(ref MessagePackWriter writer)
        {
            writer.WriteRaw(_typeReference);
        }
        public void WriteBody(ref MessagePackWriter writer, object value, PmxWriteContext context)
        {
            Invoke(_onSerializing, value, context.StreamingContext);
            writer.WriteMapHeader(_members.Length);
            foreach (var member in _members)
            {
                writer.Write(member.CombinedKey);
                member.Write(ref writer, value, context);
            }
            Invoke(_onSerialized, value, context.StreamingContext);
        }
        public void ReadBody(ref MessagePackReader reader, object value, PmxReadContext context)
        {
            Invoke(_onDeserializing, value, context.StreamingContext);
            int count = reader.ReadMapHeader();
            PmxMember member;
            for (int i = 0; i < count; i++)
            {
                if (_memberByKey.TryGetValue(reader.ReadInt32(), out member)) member.Read(ref reader, value, context);
                else reader.Skip();
            }
        }
        public void InvokeOnDeserialized(object value, StreamingContext streamingContext)
        {
            Invoke(_onDeserialized, value, streamingContext);
        }
        private static void Invoke(MethodInfo[] methods, object value, StreamingContext streamingContext)
        {
            if (methods.Length == 0) return;
            object[] arguments = new object[] { streamingContext };
            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(value, arguments);
                }
                catch (TargetInvocationException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
            }
        }
    }
}
