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
using MessagePack;

namespace CaeGlobals
{
    // A type in a file: the number of a [PmxType] type, the full name of a framework type, an array of a type, or a
    // generic type with its arguments
    //   int                       [PmxType] number
    //   string                    framework type, by full name
    //   ["[]", rank, element]     array; rank 0 is a one dimensional zero based array
    //   [definition, arguments..] generic type; the definition is a number or a name
    public static class PmxTypeCodec
    {
        // Variables                                                                                                                
        private const string ArrayMarker = "[]";
        private static readonly ConcurrentDictionary<string, Type> _typeByName = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<Type, byte[]> _bytesByType = new ConcurrentDictionary<Type, byte[]>();


        // Methods                                                                                                                  
        public static byte[] GetTypeReferenceBytes(Type type)
        {
            byte[] bytes;
            if (_bytesByType.TryGetValue(type, out bytes)) return bytes;
            //
            PmxBufferWriter buffer = new PmxBufferWriter(64);
            MessagePackWriter writer = new MessagePackWriter(buffer);
            WriteDirect(ref writer, type);
            writer.Flush();
            bytes = buffer.ToArray();
            return _bytesByType.GetOrAdd(type, bytes);
        }
        public static void Write(ref MessagePackWriter writer, Type type)
        {
            writer.WriteRaw(GetTypeReferenceBytes(type));
        }
        private static void WriteDirect(ref MessagePackWriter writer, Type type)
        {
            int id;
            if (typeof(Type).IsAssignableFrom(type)) type = typeof(Type);
            //
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                writer.WriteArrayHeader(3);
                writer.Write(ArrayMarker);
                writer.Write(type == elementType.MakeArrayType() ? 0 : type.GetArrayRank());
                WriteDirect(ref writer, elementType);
            }
            else if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type[] arguments = type.GetGenericArguments();
                writer.WriteArrayHeader(1 + arguments.Length);
                WriteDirect(ref writer, type.GetGenericTypeDefinition());
                foreach (var argument in arguments) WriteDirect(ref writer, argument);
            }
            else if (PmxTypeRegistry.TryGetId(type, out id)) writer.Write(id);
            else if (type.Assembly == typeof(object).Assembly || PmxTypeRegistry.TryGetFrameworkType(type.FullName, out _))
                writer.Write(type.FullName);
            else writer.Write(type.FullName + ", " + type.Assembly.GetName().Name);
        }
        public static Type Read(ref MessagePackReader reader)
        {
            switch (reader.NextMessagePackType)
            {
                case MessagePackType.Integer:
                    return PmxTypeRegistry.GetType(reader.ReadInt32());
                case MessagePackType.String:
                    return GetTypeByName(reader.ReadString());
                case MessagePackType.Array:
                    int count = reader.ReadArrayHeader();
                    if (reader.NextMessagePackType == MessagePackType.String)
                    {
                        MessagePackReader peek = reader.CreatePeekReader();
                        if (peek.ReadString() == ArrayMarker)
                        {
                            reader.ReadString();
                            int rank = reader.ReadInt32();
                            Type elementType = Read(ref reader);
                            return rank == 0 ? elementType.MakeArrayType() : elementType.MakeArrayType(rank);
                        }
                    }
                    Type definition = Read(ref reader);
                    Type[] arguments = new Type[count - 1];
                    for (int i = 0; i < arguments.Length; i++) arguments[i] = Read(ref reader);
                    return definition.MakeGenericType(arguments);
                default:
                    throw new PmxStorageException("The file holds a damaged type reference.");
            }
        }
        private static Type GetTypeByName(string name)
        {
            Type type;
            if (_typeByName.TryGetValue(name, out type)) return type;
            if (!PmxTypeRegistry.TryGetFrameworkType(name, out type))
                throw new PmxStorageException("The file holds an item of the unknown type " + name + ".");
            return _typeByName.GetOrAdd(name, type);
        }
    }
}
