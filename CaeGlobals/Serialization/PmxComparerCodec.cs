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
    // The comparer of a dictionary or a set decides how its keys are matched, so it is stored with it: a number for the
    // default and the string comparers, or the comparer itself when it is a [PmxType] type
    public static class PmxComparerCodec
    {
        // Variables                                                                                                                
        private const int DefaultComparer = 0;
        private static readonly StringComparer[] _stringComparers = new StringComparer[]
        {
            null, StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase, StringComparer.InvariantCulture,
            StringComparer.InvariantCultureIgnoreCase, StringComparer.CurrentCulture, StringComparer.CurrentCultureIgnoreCase
        };


        // Methods                                                                                                                  
        public static void Write<T>(ref MessagePackWriter writer, IEqualityComparer<T> comparer, PmxWriteContext context)
        {
            if (comparer == null || comparer.GetType() == EqualityComparer<T>.Default.GetType())
            {
                writer.Write(DefaultComparer);
                return;
            }
            if (comparer is StringComparer stringComparer)
            {
                for (int i = 1; i < _stringComparers.Length; i++)
                {
                    if (_stringComparers[i].Equals(stringComparer))
                    {
                        writer.Write(i);
                        return;
                    }
                }
            }
            if (!PmxClassPlan.IsStoredType(comparer.GetType()))
                throw new PmxStorageException("The comparer " + comparer.GetType().FullName + " has no storage schema.");
            PmxFormatterCache<object>.Formatter.Write(ref writer, comparer, context);
        }
        public static IEqualityComparer<T> Read<T>(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.NextMessagePackType == MessagePackType.Integer)
            {
                int code = reader.ReadInt32();
                if (code == DefaultComparer) return EqualityComparer<T>.Default;
                if (code > 0 && code < _stringComparers.Length) return (IEqualityComparer<T>)_stringComparers[code];
                throw new PmxStorageException("The file holds an unknown comparer " + code + ".");
            }
            return (IEqualityComparer<T>)PmxFormatterCache<object>.Formatter.Read(ref reader, context);
        }
    }
}
