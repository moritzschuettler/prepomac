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
    public interface IPmxFormatter
    {
        // Methods                                                                                                                  
        void WriteObject(ref MessagePackWriter writer, object value, PmxWriteContext context);
        object ReadObject(ref MessagePackReader reader, PmxReadContext context);
    }
}
