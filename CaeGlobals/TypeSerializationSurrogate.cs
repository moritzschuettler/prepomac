// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CaeGlobals
{
    // The .NET Framework serializes System.Type fields through System.UnitySerializationHolder; .NET (Core) can neither
    // serialize types nor read the holder. The surrogate writes types in the format of the .NET Framework and the holder
    // reads them back, so the files of both runtimes stay compatible.
    public static class BinaryFormatterTools
    {
        public const string UnityHolderTypeName = "System.UnitySerializationHolder";
        private const int RuntimeTypeUnity = 4;

        public static BinaryFormatter Create()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Binder = RenamedTypeSerializationBinder.Instance;
#if NET
            SurrogateSelector selector = new SurrogateSelector();
            StreamingContext context = new StreamingContext(StreamingContextStates.All);
            TypeSurrogate surrogate = new TypeSurrogate();
            selector.AddSurrogate(typeof(Type).GetType(), context, surrogate);
            // Arrays of types, such as the keys of a dictionary, have the element type System.Type
            selector.AddSurrogate(typeof(Type), context, surrogate);
            formatter.SurrogateSelector = selector;
#endif
            return formatter;
        }
        public static Type BindUnityHolder(string typeName)
        {
#if NET
            if (typeName == UnityHolderTypeName) return typeof(TypeReferenceHolder);
#endif
            return null;
        }

#if NET
        private sealed class TypeSurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Type type = (Type)obj;
                string assemblyName = type.Assembly.FullName;
                // The core library has another name in the .NET Framework
                if (type.Assembly == typeof(object).Assembly) assemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, " +
                                                                              "PublicKeyToken=b77a5c561934e089";
                info.FullTypeName = UnityHolderTypeName;
                info.AssemblyName = "mscorlib";
                info.AddValue("Data", type.FullName);
                info.AddValue("UnityType", RuntimeTypeUnity);
                info.AddValue("AssemblyName", assemblyName);
            }
            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context,
                                        ISurrogateSelector selector)
            {
                throw new NotSupportedException();
            }
        }

        [Serializable]
        private sealed class TypeReferenceHolder : ISerializable, IObjectReference
        {
            private readonly string _data;
            private readonly string _assemblyName;
            private readonly int _unityType;

            private TypeReferenceHolder(SerializationInfo info, StreamingContext context)
            {
                _unityType = info.GetInt32("UnityType");
                foreach (SerializationEntry entry in info)
                {
                    if (entry.Name == "Data") _data = entry.Value as string;
                    else if (entry.Name == "AssemblyName") _assemblyName = entry.Value as string;
                }
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotSupportedException();
            }
            public object GetRealObject(StreamingContext context)
            {
                switch (_unityType)
                {
                    case 1: return null;                    // Empty
                    case 2: return DBNull.Value;
                    case 3: return Missing.Value;
                    case RuntimeTypeUnity:
                        {
                            Type type = null;
                            if (_data != null && _assemblyName != null)
                            {
                                if (_assemblyName.StartsWith("mscorlib", StringComparison.Ordinal))
                                    type = Type.GetType(_data, false);
                                else type = Type.GetType(_data + ", " + _assemblyName, false) ?? Type.GetType(_data + ", " +
                                            new AssemblyName(_assemblyName).Name, false);
                            }
                            if (type == null && _data != null) type = RenamedTypeSerializationBinder.Instance.BindToType(
                                new AssemblyName(_assemblyName ?? "").Name, _data);
                            if (type == null) throw new SerializationException("The type " + _data + " could not be found.");
                            return type;
                        }
                    default:
                        throw new SerializationException("The serialized object of the unity type " + _unityType +
                                                         " is not supported.");
                }
            }
        }
#endif
    }
}
