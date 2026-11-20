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
    // [reference number, [items]]
    public sealed class PmxListFormatter<T> : PmxFormatter<List<T>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, List<T> value, PmxWriteContext context)
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
            writer.WriteArrayHeader(2);
            writer.Write(referenceId);
            writer.WriteArrayHeader(value.Count);
            for (int i = 0; i < value.Count; i++) formatter.Write(ref writer, value[i], context);
        }
        public override List<T> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (List<T>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            int count = reader.ReadArrayHeader();
            List<T> list = new List<T>(count);
            context.AddReference(referenceId, list);
            PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
            for (int i = 0; i < count; i++) list.Add(formatter.Read(ref reader, context));
            return list;
        }
    }
}
