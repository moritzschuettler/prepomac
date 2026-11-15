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
using System.Drawing;
using System.IO;

namespace CaeGlobals
{
    // Picks the formatter of a type: the primitives, the framework collections, the [PmxType] types, and an object or an
    // interface slot, which stores the type of the value it holds
    public static class PmxFormatterResolver
    {
        // Variables                                                                                                                
        private static readonly ConcurrentDictionary<Type, IPmxFormatter> _boxed = new ConcurrentDictionary<Type, IPmxFormatter>();
        private static readonly HashSet<Type> _primitives = new HashSet<Type>()
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(char), typeof(string),
            typeof(DateTime), typeof(TimeSpan), typeof(Guid)
        };
        private static readonly HashSet<Type> _drawingTypes = new HashSet<Type>()
        {
            typeof(Color), typeof(Size), typeof(SizeF), typeof(Point), typeof(PointF), typeof(Rectangle), typeof(RectangleF)
        };


        // Methods                                                                                                                  
        public static IPmxFormatter GetBoxed(Type type)
        {
            IPmxFormatter formatter;
            if (_boxed.TryGetValue(type, out formatter)) return formatter;
            //
            Type cacheType = typeof(PmxFormatterCache<>).MakeGenericType(type);
            formatter = (IPmxFormatter)cacheType.GetField("Formatter").GetValue(null);
            return _boxed.GetOrAdd(type, formatter);
        }
        public static bool IsPrimitive(Type type)
        {
            return _primitives.Contains(type);
        }
        public static object Create(Type type)
        {
            if (_primitives.Contains(type)) return Instantiate(typeof(PmxPrimitiveFormatter<>), type);
            if (type.IsEnum) return Instantiate(typeof(PmxEnumFormatter<>), type);
            if (_drawingTypes.Contains(type)) return Instantiate(typeof(PmxDrawingFormatter<>), type);
            if (typeof(Type).IsAssignableFrom(type)) return Instantiate(typeof(PmxTypeFormatter<>), type);
            if (type == typeof(FileInfo)) return new PmxFileInfoFormatter();
            if (typeof(Delegate).IsAssignableFrom(type)) return Instantiate(typeof(PmxDelegateFormatter<>), type);
            //
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if (type == elementType.MakeArrayType()) return Instantiate(typeof(PmxArrayFormatter<>), elementType);
                return Instantiate(typeof(PmxMultiArrayFormatter<>), type);
            }
            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                Type[] arguments = type.GetGenericArguments();
                if (definition == typeof(Nullable<>)) return Instantiate(typeof(PmxNullableFormatter<>), arguments);
                if (definition == typeof(List<>)) return Instantiate(typeof(PmxListFormatter<>), arguments);
                if (definition == typeof(HashSet<>)) return Instantiate(typeof(PmxHashSetFormatter<>), arguments);
                if (definition == typeof(LinkedList<>)) return Instantiate(typeof(PmxLinkedListFormatter<>), arguments);
                if (definition == typeof(Queue<>)) return Instantiate(typeof(PmxQueueFormatter<>), arguments);
                if (definition == typeof(Stack<>)) return Instantiate(typeof(PmxStackFormatter<>), arguments);
                if (definition == typeof(Dictionary<,>)) return Instantiate(typeof(PmxDictionaryFormatter<,>), arguments);
                if (definition == typeof(SortedDictionary<,>))
                    return Instantiate(typeof(PmxSortedDictionaryFormatter<,>), arguments);
                if (definition == typeof(KeyValuePair<,>)) return Instantiate(typeof(PmxKeyValuePairFormatter<,>), arguments);
            }
            if (PmxClassPlan.IsStoredType(type))
            {
                if (type.IsValueType) return Instantiate(typeof(PmxStructFormatter<>), type);
                return Instantiate(typeof(PmxObjectFormatter<>), type);
            }
            if (type == typeof(object) || type.IsInterface || (type.IsClass && (type.IsAbstract || !type.IsSealed)))
                return Instantiate(typeof(PmxObjectFormatter<>), type);
            return Instantiate(typeof(PmxUnsupportedFormatter<>), type);
        }
        private static object Instantiate(Type formatterDefinition, params Type[] arguments)
        {
            return Activator.CreateInstance(formatterDefinition.MakeGenericType(arguments));
        }
    }
}
