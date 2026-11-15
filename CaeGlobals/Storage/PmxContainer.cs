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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    // A file of named chunks, each compressed on its own and guarded by its own CRC-32, so that a damaged chunk is found
    // and named and the others still read:
    //   0   "PMX2"                      magic
    //   4   int32                       container version
    //   8   32 bytes ASCII              name and version of the program that wrote the file, zero padded
    //   40  int64                       offset of the chunk table
    //   48  int32                       length of the chunk table
    //   52  uint32                      CRC-32 of the chunk table
    //   56  8 bytes                     reserved
    //   64  chunks
    //   chunk table: int32 count, then per chunk: name, codec byte, int64 offset, int64 stored length, int64 raw length,
    //                uint32 CRC-32 of the stored bytes
    // All numbers are little-endian; a name is a length-prefixed UTF-8 string.
    public sealed class PmxContainer
    {
        // Variables                                                                                                                
        public const int ContainerVersion = 1;
        public const int HeaderLength = 64;
        public const int ProgramNameLength = 32;
        private static readonly byte[] _magic = Encoding.ASCII.GetBytes("PMX2");
        private readonly string _programName;
        private readonly List<PmxChunk> _chunks;
        private readonly Dictionary<string, PmxChunk> _chunkByName;


        // Properties                                                                                                               
        public string ProgramName { get { return _programName; } }
        public IReadOnlyList<PmxChunk> Chunks { get { return _chunks; } }


        // Constructors                                                                                                             
        public PmxContainer(string programName)
        {
            _programName = programName;
            _chunks = new List<PmxChunk>();
            _chunkByName = new Dictionary<string, PmxChunk>(StringComparer.Ordinal);
        }


        // Static methods                                                                                                           
        public static bool IsContainer(string fileName)
        {
            byte[] start = new byte[_magic.Length];
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Read(start, 0, start.Length) != start.Length) return false;
            }
            return IsMagic(start);
        }
        private static bool IsMagic(byte[] bytes)
        {
            for (int i = 0; i < _magic.Length; i++)
            {
                if (bytes[i] != _magic[i]) return false;
            }
            return true;
        }
        // Compresses the chunks in parallel with Zstd; a compression level of zero stores them as they are
        public static List<PmxChunk> CreateChunks(IList<KeyValuePair<string, byte[]>> rawChunks, int compressionLevel)
        {
            PmxChunk[] chunks = new PmxChunk[rawChunks.Count];
            Parallel.For(0, rawChunks.Count, i =>
            {
                byte codec = compressionLevel > 0 ? PmxChunk.CodecZstd : PmxChunk.CodecNone;
                chunks[i] = PmxChunk.Create(rawChunks[i].Key, rawChunks[i].Value, codec, compressionLevel);
            });
            return new List<PmxChunk>(chunks);
        }
        public static PmxContainer Read(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Read(stream);
        }
        public static PmxContainer Read(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
            byte[] header = reader.ReadBytes(HeaderLength);
            if (header.Length != HeaderLength || !IsMagic(header))
                throw new PmxStorageException("The file is not a PrePoMax container file.");
            int version = BitConverter.ToInt32(header, 4);
            if (version > ContainerVersion)
                throw new PmxStorageException("The file was written by a newer version of PrePoMax.");
            string programName = Encoding.ASCII.GetString(header, 8, ProgramNameLength).TrimEnd('\0');
            long tableOffset = BitConverter.ToInt64(header, 40);
            int tableLength = BitConverter.ToInt32(header, 48);
            uint tableCrc32 = BitConverter.ToUInt32(header, 52);
            //
            if (tableOffset < HeaderLength || tableOffset + tableLength > stream.Length)
                throw new PmxStorageException("The file is damaged: its table of contents is missing.");
            stream.Position = tableOffset;
            byte[] table = reader.ReadBytes(tableLength);
            if (table.Length != tableLength || PmxCrc32.Compute(table) != tableCrc32)
                throw new PmxStorageException("The file is damaged: its table of contents is damaged.");
            //
            PmxContainer container = new PmxContainer(programName);
            using (BinaryReader tableReader = new BinaryReader(new MemoryStream(table), Encoding.UTF8))
            {
                int count = tableReader.ReadInt32();
                string[] names = new string[count];
                byte[] codecs = new byte[count];
                long[] offsets = new long[count];
                long[] storedLengths = new long[count];
                long[] rawLengths = new long[count];
                uint[] crcs = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    names[i] = tableReader.ReadString();
                    codecs[i] = tableReader.ReadByte();
                    offsets[i] = tableReader.ReadInt64();
                    storedLengths[i] = tableReader.ReadInt64();
                    rawLengths[i] = tableReader.ReadInt64();
                    crcs[i] = tableReader.ReadUInt32();
                }
                for (int i = 0; i < count; i++)
                {
                    byte[] stored = new byte[0];
                    if (offsets[i] >= HeaderLength && offsets[i] + storedLengths[i] <= stream.Length)
                    {
                        stream.Position = offsets[i];
                        stored = reader.ReadBytes((int)storedLengths[i]);
                    }
                    container.Add(new PmxChunk(names[i], codecs[i], rawLengths[i], crcs[i], stored));
                }
            }
            return container;
        }
        public static string ReadProgramName(string fileName)
        {
            byte[] header = new byte[HeaderLength];
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Read(header, 0, header.Length) != header.Length || !IsMagic(header)) return null;
            }
            return Encoding.ASCII.GetString(header, 8, ProgramNameLength).TrimEnd('\0');
        }


        // Methods                                                                                                                  
        public void Add(PmxChunk chunk)
        {
            if (_chunkByName.ContainsKey(chunk.Name)) throw new ArgumentException("The chunk " + chunk.Name + " exists.");
            _chunks.Add(chunk);
            _chunkByName.Add(chunk.Name, chunk);
        }
        public bool TryGetChunk(string name, out PmxChunk chunk)
        {
            return _chunkByName.TryGetValue(name, out chunk);
        }
        public void Write(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
            long start = stream.Position;
            writer.Write(new byte[HeaderLength]);
            //
            long[] offsets = new long[_chunks.Count];
            for (int i = 0; i < _chunks.Count; i++)
            {
                offsets[i] = stream.Position - start;
                writer.Write(_chunks[i].StoredBytes);
            }
            byte[] table;
            using (MemoryStream tableStream = new MemoryStream())
            using (BinaryWriter tableWriter = new BinaryWriter(tableStream, Encoding.UTF8))
            {
                tableWriter.Write(_chunks.Count);
                for (int i = 0; i < _chunks.Count; i++)
                {
                    tableWriter.Write(_chunks[i].Name);
                    tableWriter.Write(_chunks[i].Codec);
                    tableWriter.Write(offsets[i]);
                    tableWriter.Write(_chunks[i].StoredLength);
                    tableWriter.Write(_chunks[i].RawLength);
                    tableWriter.Write(_chunks[i].Crc32);
                }
                tableWriter.Flush();
                table = tableStream.ToArray();
            }
            long tableOffset = stream.Position - start;
            writer.Write(table);
            long end = stream.Position;
            // Header
            byte[] header = new byte[HeaderLength];
            Buffer.BlockCopy(_magic, 0, header, 0, _magic.Length);
            BitConverter.GetBytes(ContainerVersion).CopyTo(header, 4);
            byte[] name = Encoding.ASCII.GetBytes(_programName ?? "");
            Buffer.BlockCopy(name, 0, header, 8, Math.Min(name.Length, ProgramNameLength));
            BitConverter.GetBytes(tableOffset).CopyTo(header, 40);
            BitConverter.GetBytes(table.Length).CopyTo(header, 48);
            BitConverter.GetBytes(PmxCrc32.Compute(table)).CopyTo(header, 52);
            stream.Position = start;
            writer.Write(header);
            stream.Position = end;
            writer.Flush();
        }
    }
}
