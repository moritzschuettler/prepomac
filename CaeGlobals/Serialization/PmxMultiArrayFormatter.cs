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
    // A multidimensional array: [reference number, [lengths], [items in row-major order]]
    public sealed class PmxMultiArrayFormatter<TArray> : PmxFormatter<TArray> where TArray : class
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, TArray value, PmxWriteContext context)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }
            int referenceId;
            if (context.TryWriteReference(ref writer, value, out referenceId)) return;
            //
            Array array = (Array)(object)value;
            IPmxFormatter formatter = PmxFormatterResolver.GetBoxed(typeof(TArray).GetElementType());
            writer.WriteArrayHeader(3);
            writer.Write(referenceId);
            writer.WriteArrayHeader(array.Rank);
            for (int i = 0; i < array.Rank; i++) writer.Write(array.GetLength(i));
            writer.WriteArrayHeader(array.Length);
            foreach (var item in array) formatter.WriteObject(ref writer, item, context);
        }
        public override TArray Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (TArray)existing;
            //
            reader.ReadArrayHeader();
            int referenceId = reader.ReadInt32();
            int[] lengths = new int[reader.ReadArrayHeader()];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = reader.ReadInt32();
            Type elementType = typeof(TArray).GetElementType();
            Array array = Array.CreateInstance(elementType, lengths);
            context.AddReference(referenceId, array);
            IPmxFormatter formatter = PmxFormatterResolver.GetBoxed(elementType);
            int count = reader.ReadArrayHeader();
            int[] indices = new int[lengths.Length];
            for (int n = 0; n < count; n++)
            {
                array.SetValue(formatter.ReadObject(ref reader, context), indices);
                for (int d = lengths.Length - 1; d >= 0; d--)
                {
                    if (++indices[d] < lengths[d]) break;
                    indices[d] = 0;
                }
            }
            return (TArray)(object)array;
        }
    }
}
