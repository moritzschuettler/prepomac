// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;

namespace CaeGlobals
{
    // [reference number, items]; an array of numbers is stored as its little-endian bytes in one binary block
    public sealed class PmxArrayFormatter<T> : PmxFormatter<T[]>
    {
        // Variables                                                                                                                
        private static readonly bool _isBulk = typeof(T) == typeof(double) || typeof(T) == typeof(int) ||
                                               typeof(T) == typeof(float) || typeof(T) == typeof(long) ||
                                               typeof(T) == typeof(byte) || typeof(T) == typeof(short) ||
                                               typeof(T) == typeof(bool) || typeof(T) == typeof(uint) ||
                                               typeof(T) == typeof(ushort) || typeof(T) == typeof(ulong) ||
                                               typeof(T) == typeof(sbyte) || typeof(T) == typeof(char);


        // Methods                                                                                                                  
        private static Span<byte> GetBytes(T[] array)
        {
            object items = array;
            if (typeof(T) == typeof(double)) return MemoryMarshal.AsBytes(new Span<double>((double[])items));
            if (typeof(T) == typeof(int)) return MemoryMarshal.AsBytes(new Span<int>((int[])items));
            if (typeof(T) == typeof(float)) return MemoryMarshal.AsBytes(new Span<float>((float[])items));
            if (typeof(T) == typeof(long)) return MemoryMarshal.AsBytes(new Span<long>((long[])items));
            if (typeof(T) == typeof(byte)) return new Span<byte>((byte[])items);
            if (typeof(T) == typeof(short)) return MemoryMarshal.AsBytes(new Span<short>((short[])items));
            if (typeof(T) == typeof(bool)) return MemoryMarshal.AsBytes(new Span<bool>((bool[])items));
            if (typeof(T) == typeof(uint)) return MemoryMarshal.AsBytes(new Span<uint>((uint[])items));
            if (typeof(T) == typeof(ushort)) return MemoryMarshal.AsBytes(new Span<ushort>((ushort[])items));
            if (typeof(T) == typeof(ulong)) return MemoryMarshal.AsBytes(new Span<ulong>((ulong[])items));
            if (typeof(T) == typeof(sbyte)) return MemoryMarshal.AsBytes(new Span<sbyte>((sbyte[])items));
            if (typeof(T) == typeof(char)) return MemoryMarshal.AsBytes(new Span<char>((char[])items));
            throw new NotSupportedException();
        }
        public override void Write(ref MessagePackWriter writer, T[] value, PmxWriteContext context)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            int referenceId;
            if (context.TryWriteReference(ref writer, value, out referenceId)) return;
            //
            writer.WriteArrayHeader(2);
            writer.Write(referenceId);
            if (_isBulk)
            {
                writer.Write(GetBytes(value));
            }
            else
            {
                PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++) formatter.Write(ref writer, value[i], context);
            }
        }
        public override T[] Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (T[])existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            T[] array;
            if (_isBulk)
            {
                ReadOnlySequence<byte> bytes = reader.ReadBytes().Value;
                array = new T[bytes.Length / Unsafe.SizeOf<T>()];
                bytes.CopyTo(GetBytes(array));
                context.AddReference(referenceId, array);
            }
            else
            {
                PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
                array = new T[reader.ReadArrayHeader()];
                context.AddReference(referenceId, array);
                for (int i = 0; i < array.Length; i++) array[i] = formatter.Read(ref reader, context);
            }
            return array;
        }
    }
}
