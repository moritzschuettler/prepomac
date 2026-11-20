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
using System.Globalization;
using System.Runtime.CompilerServices;
using MessagePack;

namespace CaeGlobals
{
    // The checks of typeof(T) are constants for the compiler of a value type, so each instance keeps only its own branch
    public sealed class PmxPrimitiveFormatter<T> : PmxFormatter<T>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            if (typeof(T) == typeof(double)) writer.Write(Unsafe.As<T, double>(ref value));
            else if (typeof(T) == typeof(int)) writer.Write(Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(bool)) writer.Write(Unsafe.As<T, bool>(ref value));
            else if (typeof(T) == typeof(string)) writer.Write((string)(object)value);
            else if (typeof(T) == typeof(float)) writer.Write(Unsafe.As<T, float>(ref value));
            else if (typeof(T) == typeof(long)) writer.Write(Unsafe.As<T, long>(ref value));
            else if (typeof(T) == typeof(byte)) writer.Write(Unsafe.As<T, byte>(ref value));
            else if (typeof(T) == typeof(sbyte)) writer.Write(Unsafe.As<T, sbyte>(ref value));
            else if (typeof(T) == typeof(short)) writer.Write(Unsafe.As<T, short>(ref value));
            else if (typeof(T) == typeof(ushort)) writer.Write(Unsafe.As<T, ushort>(ref value));
            else if (typeof(T) == typeof(uint)) writer.Write(Unsafe.As<T, uint>(ref value));
            else if (typeof(T) == typeof(ulong)) writer.Write(Unsafe.As<T, ulong>(ref value));
            else if (typeof(T) == typeof(char)) writer.Write(Unsafe.As<T, char>(ref value));
            else if (typeof(T) == typeof(decimal))
                writer.Write(Unsafe.As<T, decimal>(ref value).ToString(CultureInfo.InvariantCulture));
            else if (typeof(T) == typeof(DateTime)) writer.Write(Unsafe.As<T, DateTime>(ref value).ToBinary());
            else if (typeof(T) == typeof(TimeSpan)) writer.Write(Unsafe.As<T, TimeSpan>(ref value).Ticks);
            else if (typeof(T) == typeof(Guid)) writer.Write(Unsafe.As<T, Guid>(ref value).ToByteArray());
            else throw new NotSupportedException();
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (typeof(T) == typeof(double)) { double v = reader.ReadDouble(); return Unsafe.As<double, T>(ref v); }
            else if (typeof(T) == typeof(int)) { int v = reader.ReadInt32(); return Unsafe.As<int, T>(ref v); }
            else if (typeof(T) == typeof(bool)) { bool v = reader.ReadBoolean(); return Unsafe.As<bool, T>(ref v); }
            else if (typeof(T) == typeof(string)) return (T)(object)reader.ReadString();
            else if (typeof(T) == typeof(float)) { float v = reader.ReadSingle(); return Unsafe.As<float, T>(ref v); }
            else if (typeof(T) == typeof(long)) { long v = reader.ReadInt64(); return Unsafe.As<long, T>(ref v); }
            else if (typeof(T) == typeof(byte)) { byte v = reader.ReadByte(); return Unsafe.As<byte, T>(ref v); }
            else if (typeof(T) == typeof(sbyte)) { sbyte v = reader.ReadSByte(); return Unsafe.As<sbyte, T>(ref v); }
            else if (typeof(T) == typeof(short)) { short v = reader.ReadInt16(); return Unsafe.As<short, T>(ref v); }
            else if (typeof(T) == typeof(ushort)) { ushort v = reader.ReadUInt16(); return Unsafe.As<ushort, T>(ref v); }
            else if (typeof(T) == typeof(uint)) { uint v = reader.ReadUInt32(); return Unsafe.As<uint, T>(ref v); }
            else if (typeof(T) == typeof(ulong)) { ulong v = reader.ReadUInt64(); return Unsafe.As<ulong, T>(ref v); }
            else if (typeof(T) == typeof(char)) { char v = reader.ReadChar(); return Unsafe.As<char, T>(ref v); }
            else if (typeof(T) == typeof(decimal))
            {
                decimal v = decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
                return Unsafe.As<decimal, T>(ref v);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime v = DateTime.FromBinary(reader.ReadInt64());
                return Unsafe.As<DateTime, T>(ref v);
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                TimeSpan v = new TimeSpan(reader.ReadInt64());
                return Unsafe.As<TimeSpan, T>(ref v);
            }
            else if (typeof(T) == typeof(Guid))
            {
                Guid v = new Guid(reader.ReadBytes().Value.ToArray());
                return Unsafe.As<Guid, T>(ref v);
            }
            else throw new NotSupportedException();
        }
    }
}
