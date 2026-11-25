// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using MessagePack;

namespace CaeGlobals
{
    public sealed class PmxNullableFormatter<T> : PmxFormatter<T?> where T : struct
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T? value, PmxWriteContext context)
        {
            if (value.HasValue) PmxFormatterCache<T>.Formatter.Write(ref writer, value.Value, context);
            else writer.WriteNil();
        }
        public override T? Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            return PmxFormatterCache<T>.Formatter.Read(ref reader, context);
        }
    }
}
