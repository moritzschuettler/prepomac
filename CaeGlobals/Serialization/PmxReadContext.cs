// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MessagePack;

namespace CaeGlobals
{
    public sealed class PmxReadContext
    {
        // Variables                                                                                                                
        private readonly List<object> _objects;
        private readonly List<KeyValuePair<object, PmxClassPlan>> _completedWithCallbacks;
        private readonly StreamingContext _streamingContext;


        // Properties                                                                                                               
        public StreamingContext StreamingContext { get { return _streamingContext; } }


        // Constructors                                                                                                             
        public PmxReadContext()
        {
            _objects = new List<object>();
            _completedWithCallbacks = new List<KeyValuePair<object, PmxClassPlan>>();
            _streamingContext = new StreamingContext(StreamingContextStates.All);
        }


        // Methods                                                                                                                  
        public void AddReference(int referenceId, object value)
        {
            while (_objects.Count <= referenceId) _objects.Add(null);
            _objects[referenceId] = value;
        }
        // Reads a reference to an object read before
        public bool TryReadReference(ref MessagePackReader reader, out object value)
        {
            value = null;
            if (reader.NextMessagePackType != MessagePackType.Extension) return false;
            //
            ExtensionHeader header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != PmxWriteContext.ReferenceExtensionType || header.Length != 4)
                throw new PmxStorageException("The file holds an unknown extension type " + header.TypeCode + ".");
            ReadOnlySequence<byte> bytes = reader.ReadRaw(4);
            Span<byte> span = stackalloc byte[4];
            bytes.CopyTo(span);
            int referenceId = (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
            if (referenceId >= 0 && referenceId < _objects.Count) value = _objects[referenceId];
            // The object was written in a field this version does not know and skipped; reading on would lose it
            if (value == null)
                throw new PmxStorageException("The file holds an item this version of PrePoMax cannot read. It was " +
                                              "written by a newer version.");
            return true;
        }
        public void Completed(object value, PmxClassPlan plan)
        {
            if (plan.HasDeserializedCallbacks)
                _completedWithCallbacks.Add(new KeyValuePair<object, PmxClassPlan>(value, plan));
        }
        // As the binary formatter: the deserialized methods first, then the deserialization callbacks, each once the
        // whole graph is read
        public void RaiseDeserializedCallbacks()
        {
            foreach (var entry in _completedWithCallbacks) entry.Value.InvokeOnDeserialized(entry.Key, _streamingContext);
            foreach (var entry in _completedWithCallbacks)
            {
                if (entry.Key is IDeserializationCallback callback) callback.OnDeserialization(null);
            }
        }
    }
}
