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
    // [reference number, [key, value, key, value, ...]]; only the default key order is stored
    public sealed class PmxSortedDictionaryFormatter<TKey, TValue> : PmxFormatter<SortedDictionary<TKey, TValue>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, SortedDictionary<TKey, TValue> value, PmxWriteContext context)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            if (value.Comparer.GetType() != Comparer<TKey>.Default.GetType())
                throw new PmxStorageException("A sorted dictionary with the comparer " + value.Comparer.GetType().FullName +
                                              " cannot be stored.");
            int referenceId;
            if (context.TryWriteReference(ref writer, value, out referenceId)) return;
            //
            PmxFormatter<TKey> keyFormatter = PmxFormatterCache<TKey>.Formatter;
            PmxFormatter<TValue> valueFormatter = PmxFormatterCache<TValue>.Formatter;
            writer.WriteArrayHeader(2);
            writer.Write(referenceId);
            writer.WriteArrayHeader(value.Count * 2);
            foreach (var entry in value)
            {
                keyFormatter.Write(ref writer, entry.Key, context);
                valueFormatter.Write(ref writer, entry.Value, context);
            }
        }
        public override SortedDictionary<TKey, TValue> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (SortedDictionary<TKey, TValue>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            SortedDictionary<TKey, TValue> dictionary = new SortedDictionary<TKey, TValue>();
            context.AddReference(referenceId, dictionary);
            PmxFormatter<TKey> keyFormatter = PmxFormatterCache<TKey>.Formatter;
            PmxFormatter<TValue> valueFormatter = PmxFormatterCache<TValue>.Formatter;
            int count = reader.ReadArrayHeader() / 2;
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
