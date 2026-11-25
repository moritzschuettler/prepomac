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
    // [reference number, [items from the bottom to the top]]
    public sealed class PmxStackFormatter<T> : PmxFormatter<Stack<T>>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, Stack<T> value, PmxWriteContext context)
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
            T[] items = value.ToArray();
            writer.WriteArrayHeader(2);
            writer.Write(referenceId);
            writer.WriteArrayHeader(items.Length);
            for (int i = items.Length - 1; i >= 0; i--) formatter.Write(ref writer, items[i], context);
        }
        public override Stack<T> Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (Stack<T>)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            Stack<T> stack = new Stack<T>();
            context.AddReference(referenceId, stack);
            PmxFormatter<T> formatter = PmxFormatterCache<T>.Formatter;
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++) stack.Push(formatter.Read(ref reader, context));
            return stack;
        }
    }
}
