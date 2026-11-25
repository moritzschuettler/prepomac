// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Threading.Tasks;

namespace CaeGlobals
{
    // One named block of a container file: its bytes as stored, the codec that stored them and their CRC-32
    public sealed class PmxChunk
    {
        // Variables                                                                                                                
        public const byte CodecNone = 0;
        public const byte CodecZstd = 1;
        public const int SegmentLength = 1 << 20;
        private readonly string _name;
        private readonly byte _codec;
        private readonly long _rawLength;
        private readonly uint _crc32;
        private byte[] _storedBytes;
        private bool? _isIntact;


        // Properties                                                                                                               
        public string Name { get { return _name; } }
        public byte Codec { get { return _codec; } }
        public long RawLength { get { return _rawLength; } }
        public long StoredLength { get { return _storedBytes.Length; } }
        public uint Crc32 { get { return _crc32; } }
        public byte[] StoredBytes { get { return _storedBytes; } }


        // Constructors                                                                                                             
        public PmxChunk(string name, byte codec, long rawLength, uint crc32, byte[] storedBytes)
        {
            _name = name;
            _codec = codec;
            _rawLength = rawLength;
            _crc32 = crc32;
            _storedBytes = storedBytes;
        }


        // Methods                                                                                                                  
        // Zstd compresses the chunk as independent frames of SegmentLength bytes, so that the frames are compressed and
        // decompressed in parallel: int32 frame count, then int32 raw length and int32 stored length of each frame,
        // then the frames
        public static PmxChunk Create(string name, byte[] rawBytes, byte codec, int compressionLevel)
        {
            byte[] stored;
            if (codec == CodecZstd)
            {
                int count = Math.Max(1, (int)((rawBytes.LongLength + SegmentLength - 1) / SegmentLength));
                byte[][] frames = new byte[count][];
                Parallel.For(0, count, i =>
                {
                    int offset = i * SegmentLength;
                    int length = Math.Min(SegmentLength, rawBytes.Length - offset);
                    using (ZstdSharp.Compressor compressor = new ZstdSharp.Compressor(compressionLevel))
                        frames[i] = compressor.Wrap(new ReadOnlySpan<byte>(rawBytes, offset, length)).ToArray();
                });
                long storedLength = 4 + 8L * count;
                foreach (var frame in frames) storedLength += frame.Length;
                stored = new byte[storedLength];
                BitConverter.GetBytes(count).CopyTo(stored, 0);
                int position = 4 + 8 * count;
                for (int i = 0; i < count; i++)
                {
                    BitConverter.GetBytes(Math.Min(SegmentLength, rawBytes.Length - i * SegmentLength)).CopyTo(stored, 4 + 8 * i);
                    BitConverter.GetBytes(frames[i].Length).CopyTo(stored, 8 + 8 * i);
                    Buffer.BlockCopy(frames[i], 0, stored, position, frames[i].Length);
                    position += frames[i].Length;
                }
            }
            else if (codec == CodecNone) stored = rawBytes;
            else throw new ArgumentOutOfRangeException(nameof(codec));
            PmxChunk chunk = new PmxChunk(name, codec, rawBytes.LongLength, PmxCrc32.Compute(stored), stored);
            chunk._isIntact = true;
            return chunk;
        }
        public bool IsIntact()
        {
            if (!_isIntact.HasValue) _isIntact = PmxCrc32.Compute(_storedBytes) == _crc32;
            return _isIntact.Value;
        }
        public byte[] GetRawBytes()
        {
            if (!IsIntact()) throw new PmxStorageException("The part '" + _name + "' of the file is damaged.");
            if (_codec == CodecNone) return _storedBytes;
            if (_codec != CodecZstd)
                throw new PmxStorageException("The part '" + _name + "' of the file uses the unknown codec " + _codec + ".");
            //
            byte[] raw = new byte[_rawLength];
            int count = BitConverter.ToInt32(_storedBytes, 0);
            int[] rawOffsets = new int[count];
            int[] storedOffsets = new int[count];
            int rawOffset = 0;
            int storedOffset = 4 + 8 * count;
            for (int i = 0; i < count; i++)
            {
                rawOffsets[i] = rawOffset;
                storedOffsets[i] = storedOffset;
                rawOffset += BitConverter.ToInt32(_storedBytes, 4 + 8 * i);
                storedOffset += BitConverter.ToInt32(_storedBytes, 8 + 8 * i);
            }
            if (rawOffset != _rawLength || storedOffset != _storedBytes.Length)
                throw new PmxStorageException("The part '" + _name + "' of the file is damaged.");
            bool isDamaged = false;
            Parallel.For(0, count, i =>
            {
                int rawLength = BitConverter.ToInt32(_storedBytes, 4 + 8 * i);
                int storedLength = BitConverter.ToInt32(_storedBytes, 8 + 8 * i);
                using (ZstdSharp.Decompressor decompressor = new ZstdSharp.Decompressor())
                {
                    int written = decompressor.Unwrap(new ReadOnlySpan<byte>(_storedBytes, storedOffsets[i], storedLength),
                                                      new Span<byte>(raw, rawOffsets[i], rawLength));
                    if (written != rawLength) isDamaged = true;
                }
            });
            if (isDamaged) throw new PmxStorageException("The part '" + _name + "' of the file is damaged.");
            return raw;
        }
        // Lets the stored bytes go once the chunk was read, so that a large result is not kept twice
        public void Release()
        {
            _storedBytes = new byte[0];
        }
    }
}
