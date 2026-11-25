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
    // [reference number, comparer, [items]]
    public sealed class PmxHashSetFormatter<T> : PmxFormatter<HashSet<T>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, HashSet<T> value, PmxWriteContext context)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            int referenceId;
            if (context.TryWriteReference(ref writer, value, out referenceId)) return;
            //
            PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
            writer.WriteArrayHeader(3);
            writer.Write(referenceId);
            PmxComparerCodec.Write(ref writer, value.Comparer, context);
            writer.WriteArrayHeader(value.Count);
            foreach (var item in value) formatter.Write(ref writer, item, context);
        }
        public override HashSet<T> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (HashSet<T>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            HashSet<T> set = new HashSet<T>(PmxComparerCodec.Read<T>(ref reader, context));
            context.AddReference(referenceId, set);
            PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++) set.Add(formatter.Read(ref reader, context));
            return set;
        }
    }
}
