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
using System.IO;
using System.Text;
using CaeGlobals;
using MessagePack;

namespace PmxTool
{
    // Prints a container file, a history journal or a storage payload as text. A stored object is printed as
    // {"$type": "<name> #<number>", "$id": <reference number>, "<field>": value, ...}, an object written before as
    // {"$ref": <reference number>}, and a binary block by its length.
    public sealed class PayloadDumper
    {
        // Variables                                                                                                                
        private readonly StringBuilder _text;
        private readonly Dictionary<int, string> _fieldNames;
        private readonly int _maxItems;


        // Constructors                                                                                                             
        public PayloadDumper(int maxItems)
        {
            _text = new StringBuilder();
            _fieldNames = new Dictionary<int, string>();
            _maxItems = maxItems;
        }


        // Static methods                                                                                                           
        public static int Run(string fileName, string chunkName, int maxItems, TextWriter output)
        {
            if (PmxContainer.IsContainer(fileName))
            {
                PmxContainer container = PmxContainer.Read(fileName);
                output.WriteLine("Container written by " + container.ProgramName);
                foreach (var chunk in container.Chunks)
                {
                    output.WriteLine(string.Format("  {0,-24} codec {1}  stored {2,12:N0}  raw {3,12:N0}  crc {4:X8} {5}",
                                                   chunk.Name, chunk.Codec, chunk.StoredLength, chunk.RawLength, chunk.Crc32,
                                                   chunk.IsIntact() ? "ok" : "DAMAGED"));
                }
                foreach (var chunk in container.Chunks)
                {
                    if (chunkName != null && chunk.Name != chunkName) continue;
                    byte[] raw = chunk.GetRawBytes();
                    if (!PmxObjectSerializer.IsPayload(raw, 0, raw.Length)) continue;
                    output.WriteLine();
                    output.WriteLine("== " + chunk.Name);
                    output.WriteLine(new PayloadDumper(maxItems).Dump(raw, 0, raw.Length));
                }
                return 0;
            }
            byte[] bytes = File.ReadAllBytes(fileName);
            if (PmxJournal.IsJournal(fileName))
            {
                int position = 8;
                int frame = 0;
                while (position + 8 <= bytes.Length)
                {
                    int length = BitConverter.ToInt32(bytes, position);
                    uint crc32 = BitConverter.ToUInt32(bytes, position + 4);
                    bool intact = length >= 0 && position + 8 + length <= bytes.Length &&
                                  PmxCrc32.Compute(bytes, position + 8, length) == crc32;
                    output.WriteLine("== frame " + frame++ + " (" + length + " bytes" + (intact ? ")" : ", DAMAGED)"));
                    if (!intact) break;
                    output.WriteLine(new PayloadDumper(maxItems).Dump(bytes, position + 8, length));
                    position += 8 + length;
                }
                return 0;
            }
            if (PmxObjectSerializer.IsPayload(bytes, 0, bytes.Length))
            {
                output.WriteLine(new PayloadDumper(maxItems).Dump(bytes, 0, bytes.Length));
                return 0;
            }
            output.WriteLine("The file is neither a container, a history journal nor a storage payload.");
            return 1;
        }


        // Methods                                                                                                                  
        public string Dump(byte[] bytes, int offset, int count)
        {
            MessagePackReader reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset + 5, count - 5));
            WriteValue(ref reader, 0);
            return _text.ToString();
        }
        private void Indent(int depth)
        {
            _text.Append('\n').Append(' ', depth * 2);
        }
        private void WriteValue(ref MessagePackReader reader, int depth)
        {
            switch (reader.NextMessagePackType)
            {
                case MessagePackType.Nil:
                    reader.ReadNil();
                    _text.Append("null");
                    break;
                case MessagePackType.Boolean:
                    _text.Append(reader.ReadBoolean() ? "true" : "false");
                    break;
                case MessagePackType.Integer:
                    _text.Append(reader.ReadInt64());
                    break;
                case MessagePackType.Float:
                    _text.Append(reader.ReadDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case MessagePackType.String:
                    _text.Append('"').Append(Escape(reader.ReadString())).Append('"');
                    break;
                case MessagePackType.Binary:
                    _text.Append("\"<").Append(reader.ReadBytes().Value.Length).Append(" bytes>\"");
                    break;
                case MessagePackType.Extension:
                    ExtensionHeader header = reader.ReadExtensionFormatHeader();
                    ReadOnlySequence<byte> data = reader.ReadRaw(header.Length);
                    byte[] id = data.ToArray();
                    if (header.TypeCode == PmxWriteContext.ReferenceExtensionType && id.Length == 4)
                        _text.Append("{\"$ref\": ").Append((id[0] << 24) | (id[1] << 16) | (id[2] << 8) | id[3]).Append('}');
                    else _text.Append("\"<extension ").Append(header.TypeCode).Append('>').Append('"');
                    break;
                case MessagePackType.Array:
                    WriteArray(ref reader, depth);
                    break;
                case MessagePackType.Map:
                    WriteMap(ref reader, depth, null);
                    break;
                default:
                    reader.Skip();
                    _text.Append("\"?\"");
                    break;
            }
        }
        private void WriteArray(ref MessagePackReader reader, int depth)
        {
            // A stored object is [type, reference number, {key: value}]
            MessagePackReader peek = reader.CreatePeekReader();
            int count = peek.ReadArrayHeader();
            if (count == 3)
            {
                MessagePackReader probe = peek.CreatePeekReader();
                MessagePackType first = probe.NextMessagePackType;
                probe.Skip();
                if (probe.NextMessagePackType == MessagePackType.Integer && (first == MessagePackType.Integer ||
                                                                             first == MessagePackType.Array))
                {
                    probe.Skip();
                    if (probe.NextMessagePackType == MessagePackType.Map)
                    {
                        reader.ReadArrayHeader();
                        Type type = null;
                        try
                        {
                            type = PmxTypeCodec.Read(ref reader);
                        }
                        catch (PmxStorageException)
                        {
                        }
                        int referenceId = reader.ReadInt32();
                        string typeName = type != null ? type.FullName : "unknown";
                        int typeId;
                        if (type != null && PmxTypeRegistry.TryGetId(type.IsGenericType ? type.GetGenericTypeDefinition() : type,
                                                                     out typeId))
                            typeName += " #" + typeId;
                        WriteMap(ref reader, depth, "\"$type\": \"" + typeName + "\", \"$id\": " + referenceId);
                        return;
                    }
                }
            }
            count = reader.ReadArrayHeader();
            _text.Append('[');
            for (int i = 0; i < count; i++)
            {
                if (i >= _maxItems)
                {
                    reader.Skip();
                    if (i == _maxItems) _text.Append(", \"... ").Append(count - _maxItems).Append(" more\"");
                    continue;
                }
                if (i > 0) _text.Append(", ");
                WriteValue(ref reader, depth + 1);
            }
            _text.Append(']');
        }
        private void WriteMap(ref MessagePackReader reader, int depth, string header)
        {
            int count = reader.ReadMapHeader();
            _text.Append('{');
            if (header != null)
            {
                Indent(depth + 1);
                _text.Append(header);
            }
            for (int i = 0; i < count; i++)
            {
                if (header != null || i > 0) _text.Append(',');
                Indent(depth + 1);
                if (reader.NextMessagePackType == MessagePackType.Integer && header != null)
                    _text.Append('"').Append(GetFieldName(reader.ReadInt32())).Append('"');
                else WriteValue(ref reader, depth + 1);
                _text.Append(": ");
                WriteValue(ref reader, depth + 1);
            }
            Indent(depth);
            _text.Append('}');
        }
        private string GetFieldName(int combinedKey)
        {
            string name;
            if (_fieldNames.TryGetValue(combinedKey, out name)) return name;
            int levelId = combinedKey >> 10;
            int key = combinedKey & 1023;
            name = levelId + "." + key;
            try
            {
                Type level = PmxTypeRegistry.GetType(levelId);
                int id;
                foreach (var entry in PmxClassPlan.GetStoredFields(level, out id))
                {
                    if (entry.Value == key)
                    {
                        name = entry.Key.Name.StartsWith("<") ? entry.Key.Name.Substring(1, entry.Key.Name.IndexOf('>') - 1)
                                                              : entry.Key.Name;
                        break;
                    }
                }
            }
            catch (PmxStorageException)
            {
            }
            _fieldNames[combinedKey] = name;
            return name;
        }
        private static string Escape(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
