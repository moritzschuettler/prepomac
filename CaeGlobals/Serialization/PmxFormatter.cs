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
    public abstract class PmxFormatter<T> : IPmxFormatter
    {
        // Methods                                                                                                                  
        public abstract void Write(ref MessagePackWriter writer, T value, PmxWriteContext context);
        public abstract T Read(ref MessagePackReader reader, PmxReadContext context);
        public void WriteObject(ref MessagePackWriter writer, object value, PmxWriteContext context)
        {
            Write(ref writer, (T)value, context);
        }
        public object ReadObject(ref MessagePackReader reader, PmxReadContext context)
        {
            return Read(ref reader, context);
        }
    }
}
