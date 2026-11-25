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
    // A type value is stored as a type reference, by its [PmxType] number where it has one
    public sealed class PmxTypeFormatter<T> : PmxFormatter<T> where T : Type
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            if (value == null) writer.WriteNil();
            else PmxTypeCodec.Write(ref writer, value);
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            return (T)PmxTypeCodec.Read(ref reader);
        }
    }
}
