// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using MessagePack;

namespace CaeGlobals
{
    // [key, value]
    public sealed class PmxKeyValuePairFormatter<TKey, TValue> : PmxFormatter<KeyValuePair<TKey, TValue>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, KeyValuePair<TKey, TValue> value, PmxWriteContext context)
        {
            writer.WriteArrayHeader(2);
            PmxFormatterCache<TKey>.Formatter.Write(ref writer, value.Key, context);
            PmxFormatterCache<TValue>.Formatter.Write(ref writer, value.Value, context);
        }
        public override KeyValuePair<TKey, TValue> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            reader.ReadArrayHeader();
            TKey key = PmxFormatterCache<TKey>.Formatter.Read(ref reader, context);
            TValue value = PmxFormatterCache<TValue>.Formatter.Read(ref reader, context);
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}
