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
using System.IO;

namespace CaeGlobals
{
    public sealed class PmxBufferWriter : IBufferWriter<byte>
    {
        // Variables                                                                                                                
        private byte[] _buffer;
        private int _length;


        // Properties                                                                                                               
        public int Length { get { return _length; } }
        public byte[] Buffer { get { return _buffer; } }


        // Constructors                                                                                                             
        public PmxBufferWriter(int initialCapacity = 4096)
        {
            _buffer = new byte[Math.Max(initialCapacity, 256)];
            _length = 0;
        }


        // Methods                                                                                                                  
        public void Advance(int count)
        {
            if (count < 0 || _length + count > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
            _length += count;
        }
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Memory<byte>(_buffer, _length, _buffer.Length - _length);
        }
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Span<byte>(_buffer, _length, _buffer.Length - _length);
        }
        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint < 1) sizeHint = 1;
            if (_buffer.Length - _length >= sizeHint) return;
            //
            long newSize = Math.Max((long)_buffer.Length * 2, (long)_length + sizeHint);
            if (newSize > int.MaxValue - 64) newSize = Math.Max((long)_length + sizeHint, int.MaxValue - 64);
            if (newSize < _length + sizeHint) throw new PmxStorageException("The stored data exceeds 2 GB.");
            Array.Resize(ref _buffer, (int)newSize);
        }
        public void Write(byte[] bytes)
        {
            EnsureCapacity(bytes.Length);
            System.Buffer.BlockCopy(bytes, 0, _buffer, _length, bytes.Length);
            _length += bytes.Length;
        }
        public byte[] ToArray()
        {
            byte[] bytes = new byte[_length];
            System.Buffer.BlockCopy(_buffer, 0, bytes, 0, _length);
            return bytes;
        }
        public void WriteTo(Stream stream)
        {
            stream.Write(_buffer, 0, _length);
        }
    }
}
