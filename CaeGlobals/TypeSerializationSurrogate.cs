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
            // File and directory infos are not serializable in .NET (Core); they are written as the .NET Framework does
            selector.AddSurrogate(typeof(System.IO.FileInfo), context, new FileSystemInfoSurrogate());
            selector.AddSurrogate(typeof(System.IO.DirectoryInfo), context, new FileSystemInfoSurrogate());
            formatter.SurrogateSelector = new DelegateSurrogateSelector(selector);
#endif
            return formatter;
        }
#if NET
        // The type of an assembly qualified name, the assemblies without a strong name (PublicKeyToken=null) taken in any
        // version, the type arguments of a generic type included
        public static Type ResolveIgnoringVersions(string assemblyName, string typeName)
        {
            try
            {
                string qualified = typeName + ", " + assemblyName;
                return Type.GetType(qualified, LoadAssembly, null, false);
            }
            catch
            {
                return null;
            }
        }
        private static System.Reflection.Assembly LoadAssembly(System.Reflection.AssemblyName name)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase)) return assembly;
            }
            try { return System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(name.Name)); }
            catch { return null; }
        }
#endif
        public static Type BindUnityHolder(string typeName)
        {
#if NET
            if (typeName == UnityHolderTypeName) return typeof(TypeReferenceHolder);
            if (typeName == "System.IO.FileInfo") return typeof(FileInfoHolder);
            if (typeName == "System.IO.DirectoryInfo") return typeof(DirectoryInfoHolder);
            // The .NET Framework writes the subscribers of events that were not marked [field: NonSerialized]
            if (typeName == "System.DelegateSerializationHolder") return typeof(DelegateHolder);
            if (typeName == "System.DelegateSerializationHolder+DelegateEntry") return typeof(DelegateEntryHolder);
            if (typeName == "System.Reflection.MemberInfoSerializationHolder") return typeof(MemberInfoHolder);
#endif
            return null;
        }

#if NET
        // Delegates cannot be serialized by .NET (Core). An event that was not marked [field: NonSerialized] is written
        // as an empty reference and read back without subscribers, which is what the reloaded objects start with
        private sealed class DelegateSurrogateSelector : ISurrogateSelector
        {
            private readonly ISurrogateSelector _inner;
            private readonly DelegateSurrogate _delegateSurrogate = new DelegateSurrogate();
            public DelegateSurrogateSelector(ISurrogateSelector inner) { _inner = inner; }
            public void ChainSelector(ISurrogateSelector selector) { _inner.ChainSelector(selector); }
            public ISurrogateSelector GetNextSelector() { return _inner.GetNextSelector(); }
            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                if (typeof(Delegate).IsAssignableFrom(type))
                {
                    selector = this;
                    return _delegateSurrogate;
                }
                return _inner.GetSurrogate(type, context, out selector);
            }
        }
        private sealed class DelegateSurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                if (Environment.GetEnvironmentVariable("PPM_TRACE") == "1")
                {
                    Delegate value = (Delegate)obj;
                    Console.Error.WriteLine("[serialization] delegate " + obj.GetType() + " -> " +
                                            value.Method?.DeclaringType + "." + value.Method?.Name);
                }
                info.FullTypeName = UnityHolderTypeName;
                info.AssemblyName = "mscorlib";
                info.AddValue("Data", (string)null);
                info.AddValue("UnityType", 1);
                info.AddValue("AssemblyName", (string)null);
            }
            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context,
                                        ISurrogateSelector selector)
            {
                throw new NotSupportedException();
            }
        }
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

        // System.IO.FileInfo and DirectoryInfo of the .NET Framework: ISerializable with the original and the full path
        private sealed class FileSystemInfoSurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                System.IO.FileSystemInfo fileSystemInfo = (System.IO.FileSystemInfo)obj;
                info.FullTypeName = obj is System.IO.FileInfo ? "System.IO.FileInfo" : "System.IO.DirectoryInfo";
                info.AssemblyName = "mscorlib";
                info.AddValue("OriginalPath", fileSystemInfo.ToString());
                info.AddValue("FullPath", fileSystemInfo.FullName);
            }
            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context,
                                        ISurrogateSelector selector)
            {
                throw new NotSupportedException();
            }
        }
        [Serializable]
        private sealed class FileInfoHolder : ISerializable, IObjectReference
        {
            private readonly string _path;
            private FileInfoHolder(SerializationInfo info, StreamingContext context) { _path = ReadPath(info); }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { throw new NotSupportedException(); }
            public object GetRealObject(StreamingContext context) { return new System.IO.FileInfo(_path); }
        }
        [Serializable]
        private sealed class DirectoryInfoHolder : ISerializable, IObjectReference
        {
            private readonly string _path;
            private DirectoryInfoHolder(SerializationInfo info, StreamingContext context) { _path = ReadPath(info); }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { throw new NotSupportedException(); }
            public object GetRealObject(StreamingContext context) { return new System.IO.DirectoryInfo(_path); }
        }
        // A delegate of the .NET Framework is read back as a delegate of its type that does nothing: its subscribers were
        // the objects of the session that wrote it, and the objects read back subscribe again where they are used
        [Serializable]
        private sealed class DelegateHolder : ISerializable, IObjectReference
        {
            private readonly DelegateEntryHolder _entry;
            private DelegateHolder(SerializationInfo info, StreamingContext context)
            {
                foreach (SerializationEntry entry in info)
                {
                    if (entry.Name == "Delegate") _entry = entry.Value as DelegateEntryHolder;
                }
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { throw new NotSupportedException(); }
            public object GetRealObject(StreamingContext context)
            {
                // Null asks to be called again once the entry is read
                if (_entry == null || _entry.TypeName == null) return null;
                Type type = RenamedTypeSerializationBinder.Instance.BindToType(_entry.AssemblyName ?? "", _entry.TypeName) ??
                            Type.GetType(_entry.TypeName, false);
                if (type == null || !typeof(Delegate).IsAssignableFrom(type))
                    throw new SerializationException("The delegate type " + _entry.TypeName + " could not be found.");
                return CreateEmptyDelegate(type);
            }
        }
        private static Delegate CreateEmptyDelegate(Type type)
        {
            MethodInfo invoke = type.GetMethod("Invoke");
            System.Linq.Expressions.ParameterExpression[] parameters =
                Array.ConvertAll(invoke.GetParameters(), p => System.Linq.Expressions.Expression.Parameter(p.ParameterType, p.Name));
            System.Linq.Expressions.Expression body = invoke.ReturnType == typeof(void) ?
                (System.Linq.Expressions.Expression)System.Linq.Expressions.Expression.Empty() :
                System.Linq.Expressions.Expression.Default(invoke.ReturnType);
            return System.Linq.Expressions.Expression.Lambda(type, body, parameters).Compile();
        }
        [Serializable]
        private sealed class DelegateEntryHolder : ISerializable
        {
            public readonly string TypeName;
            public readonly string AssemblyName;
            private DelegateEntryHolder(SerializationInfo info, StreamingContext context)
            {
                foreach (SerializationEntry entry in info)
                {
                    if (entry.Name == "type") TypeName = entry.Value as string;
                    else if (entry.Name == "assembly") AssemblyName = entry.Value as string;
                }
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { throw new NotSupportedException(); }
        }
        [Serializable]
        private sealed class MemberInfoHolder : ISerializable
        {
            private MemberInfoHolder(SerializationInfo info, StreamingContext context) { }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { throw new NotSupportedException(); }
        }
        private static string ReadPath(SerializationInfo info)
        {
            string fullPath = null, originalPath = null;
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == "FullPath") fullPath = entry.Value as string;
                else if (entry.Name == "OriginalPath") originalPath = entry.Value as string;
            }
            return fullPath ?? originalPath ?? "";
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
