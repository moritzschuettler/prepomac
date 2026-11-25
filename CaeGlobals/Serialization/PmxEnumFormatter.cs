// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.CompilerServices;
using MessagePack;

namespace CaeGlobals
{
    // An enumeration is stored by its number, so its members may be renamed but never renumbered
    public sealed class PmxEnumFormatter<T> : PmxFormatter<T> where T : struct
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            int size = Unsafe.SizeOf<T>();
            if (size == 4) writer.Write(Unsafe.As<T, int>(ref value));
            else if (size == 1) writer.Write(Unsafe.As<T, byte>(ref value));
            else if (size == 2) writer.Write(Unsafe.As<T, short>(ref value));
            else writer.Write(Unsafe.As<T, long>(ref value));
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            int size = Unsafe.SizeOf<T>();
            if (size == 4) { int v = reader.ReadInt32(); return Unsafe.As<int, T>(ref v); }
            else if (size == 1) { byte v = unchecked((byte)reader.ReadInt64()); return Unsafe.As<byte, T>(ref v); }
            else if (size == 2) { short v = unchecked((short)reader.ReadInt64()); return Unsafe.As<short, T>(ref v); }
            else { long v = reader.ReadInt64(); return Unsafe.As<long, T>(ref v); }
        }
    }
}
