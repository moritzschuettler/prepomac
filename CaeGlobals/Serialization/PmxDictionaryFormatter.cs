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
    // [reference number, comparer, [key, value, key, value, ...]] in the order the dictionary enumerates its entries
    public sealed class PmxDictionaryFormatter<TKey, TValue> : PmxFormatter<Dictionary<TKey, TValue>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, Dictionary<TKey, TValue> value, PmxWriteContext context)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            int referenceId;
            if (context.TryWriteReference(ref writer, value, out referenceId)) return;
            //
            PmxFormatter<TKey> keyFormatter = PmxFormatterCache<TKey>.Formatter;
            PmxFormatter<TValue> valueFormatter = PmxFormatterCache<TValue>.Formatter;
            writer.WriteArrayHeader(3);
            writer.Write(referenceId);
            PmxComparerCodec.Write(ref writer, value.Comparer, context);
            writer.WriteArrayHeader(value.Count * 2);
            foreach (var entry in value)
            {
                keyFormatter.Write(ref writer, entry.Key, context);
                valueFormatter.Write(ref writer, entry.Value, context);
            }
        }
        public override Dictionary<TKey, TValue> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (Dictionary<TKey, TValue>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            IEqualityComparer<TKey> comparer = PmxComparerCodec.Read<TKey>(ref reader, context);
            int count = reader.ReadArrayHeader() / 2;
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>(count, comparer);
            context.AddReference(referenceId, dictionary);
            PmxFormatter<TKey> keyFormatter = PmxFormatterCache<TKey>.Formatter;
            PmxFormatter<TValue> valueFormatter = PmxFormatterCache<TValue>.Formatter;
            TKey key;
            for (int i = 0; i < count; i++)
            {
                key = keyFormatter.Read(ref reader, context);
                dictionary[key] = valueFormatter.Read(ref reader, context);
            }
            return dictionary;
        }
    }
}
