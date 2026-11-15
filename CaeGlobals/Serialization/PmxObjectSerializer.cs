// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.IO;
using MessagePack;

namespace CaeGlobals
{
    // Stores an object graph as MessagePack under the [PmxType] numbers and [PmxKey] keys of its types. A payload
    // starts with the magic "PMXS" and a format version byte, followed by one value written by the object formatter.
    public sealed class PmxObjectSerializer : IObjectSerializer
    {
        // Variables                                                                                                                
        public const byte FormatVersion = 1;
        private static readonly byte[] _magic = new byte[] { (byte)'P', (byte)'M', (byte)'X', (byte)'S', FormatVersion };


        // Methods                                                                                                                  
        public static bool IsPayload(byte[] bytes, int offset, int count)
        {
            if (count < _magic.Length - 1) return false;
            for (int i = 0; i < _magic.Length - 1; i++)
            {
                if (bytes[offset + i] != _magic[i]) return false;
            }
            return true;
        }
        public T Clone<T>(T value)
        {
            if (value == null) return value;
            byte[] bytes = SerializeToBytes(value);
            return (T)DeserializeFromBytes(bytes, 0, bytes.Length);
        }
        public byte[] SerializeToBytes(object value)
        {
            PmxBufferWriter buffer = new PmxBufferWriter();
            WriteTo(buffer, value);
            return buffer.ToArray();
        }
        public void WriteTo(PmxBufferWriter buffer, object value)
        {
            buffer.Write(_magic);
            MessagePackWriter writer = new MessagePackWriter(buffer);
            PmxFormatterCache<object>.Formatter.Write(ref writer, value, new PmxWriteContext());
            writer.Flush();
        }
        public object DeserializeFromBytes(byte[] bytes, int offset, int count)
        {
            if (!IsPayload(bytes, offset, count)) throw new PmxStorageException("The data is not a PrePoMax storage payload.");
            if (bytes[offset + _magic.Length - 1] > FormatVersion)
                throw new PmxStorageException("The data was written by a newer version of PrePoMax.");
            //
            MessagePackReader reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset + _magic.Length,
                                                                                      count - _magic.Length));
            PmxReadContext context = new PmxReadContext();
            object value = PmxFormatterCache<object>.Formatter.Read(ref reader, context);
            context.RaiseDeserializedCallbacks();
            return value;
        }
        public void Serialize(Stream stream, object value)
        {
            PmxBufferWriter buffer = new PmxBufferWriter();
            WriteTo(buffer, value);
            buffer.WriteTo(stream);
        }
        // A stream that is not a storage payload was written by an older version and goes to the legacy reader
        public object Deserialize(Stream stream)
        {
            byte[] bytes;
            if (stream is MemoryStream memoryStream && memoryStream.Position == 0) bytes = memoryStream.ToArray();
            else
            {
                using (MemoryStream copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    bytes = copy.ToArray();
                }
            }
            if (!IsPayload(bytes, 0, bytes.Length))
            {
                if (ObjectSerializer.LegacyReader == null)
                    throw new PmxStorageException("The data was written by an older version of PrePoMax, which this build " +
                                                  "cannot read.");
                using (MemoryStream legacy = new MemoryStream(bytes)) return ObjectSerializer.LegacyReader(legacy);
            }
            return DeserializeFromBytes(bytes, 0, bytes.Length);
        }
    }
}
