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
using System.Runtime.Serialization;
using MessagePack;

namespace CaeGlobals
{
    // The state of one write: every object written so far gets a number, and an object met again is written as a
    // reference to that number, so shared objects and cycles come back as they were
    public sealed class PmxWriteContext
    {
        // Variables                                                                                                                
        public const sbyte ReferenceExtensionType = 1;
        private readonly Dictionary<object, int> _referenceIds;
        private readonly StreamingContext _streamingContext;
        private int _nextReferenceId;


        // Properties                                                                                                               
        public StreamingContext StreamingContext { get { return _streamingContext; } }


        // Constructors                                                                                                             
        public PmxWriteContext()
        {
            _referenceIds = new Dictionary<object, int>(PmxReferenceComparer.Instance);
            _streamingContext = new StreamingContext(StreamingContextStates.All);
            _nextReferenceId = 0;
        }


        // Methods                                                                                                                  
        // Writes a reference to an object written before and returns true, or numbers a new object and returns false
        public bool TryWriteReference(ref MessagePackWriter writer, object value, out int referenceId)
        {
            if (TryWriteExistingReference(ref writer, value))
            {
                referenceId = -1;
                return true;
            }
            referenceId = AddReference(value);
            return false;
        }
        public bool TryWriteExistingReference(ref MessagePackWriter writer, object value)
        {
            int referenceId;
            if (!_referenceIds.TryGetValue(value, out referenceId)) return false;
            //
            writer.WriteExtensionFormatHeader(new ExtensionHeader(ReferenceExtensionType, 4));
            Span<byte> span = writer.GetSpan(4);
            span[0] = (byte)(referenceId >> 24);
            span[1] = (byte)(referenceId >> 16);
            span[2] = (byte)(referenceId >> 8);
            span[3] = (byte)referenceId;
            writer.Advance(4);
            return true;
        }
        public int AddReference(object value)
        {
            int referenceId = _nextReferenceId++;
            _referenceIds.Add(value, referenceId);
            return referenceId;
        }
    }
}
