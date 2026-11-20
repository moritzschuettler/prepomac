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
    // A [PmxType] structure is stored as its {key: value} map; it has no identity, so it is never referenced
    public sealed class PmxStructFormatter<T> : PmxFormatter<T> where T : struct
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            PmxClassPlan.Get(typeof(T)).WriteBody(ref writer, value, context);
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            PmxClassPlan plan = PmxClassPlan.Get(typeof(T));
            object boxed = plan.CreateUninitialized();
            plan.ReadBody(ref reader, boxed, context);
            return (T)boxed;
        }
    }
}
