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
    // A delegate is never stored: whom it calls belongs to the running session
    public sealed class PmxDelegateFormatter<T> : PmxFormatter<T> where T : class
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            writer.WriteNil();
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            reader.Skip();
            return null;
        }
    }
}
