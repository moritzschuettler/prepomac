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
    // [reference number, [items from first to last]]
    public sealed class PmxLinkedListFormatter<T> : PmxFormatter<LinkedList<T>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, LinkedList<T> value, PmxWriteContext context)
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
            foreach (var item in value) formatter.Write(ref writer, item, context);
        }
        public override LinkedList<T> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (LinkedList<T>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            LinkedList<T> list = new LinkedList<T>();
            context.AddReference(referenceId, list);
            PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++) list.AddLast(formatter.Read(ref reader, context));
            return list;
        }
    }
}
