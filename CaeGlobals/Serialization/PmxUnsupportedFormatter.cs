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
    // A type without a storage schema can be stored only while the slot is empty, as the binary formatter allowed it
    public sealed class PmxUnsupportedFormatter<T> : PmxFormatter<T>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            if (value == null) writer.WriteNil();
            else throw new PmxStorageException("The type " + typeof(T).FullName + " has no storage schema.");
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            reader.Skip();
            return default(T);
        }
    }
}
