// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CaeResults
{
    internal class FrdStreamReader
    {
        // Variables
        private Stream _stream;
        private byte[] _buffer;
        private int _position;
        private int _length;
        private bool _endOfStream;


        // Constructors
        public FrdStreamReader(Stream stream)
        {
            _stream = stream;
            _buffer = new byte[1 << 20];
            _position = 0;
            _length = 0;
            _endOfStream = false;
        }


        // Methods
        public string ReadLine()
        {
            int lineEnd;
            int searchStart = _position;
            while (true)
            {
                lineEnd = Array.IndexOf(_buffer, (byte)'\n', searchStart, _length - searchStart);
                if (lineEnd >= 0) break;
                //
                searchStart = _length - _position;
                if (!FillBuffer())
                {
                    if (_position == _length) return null;
                    string lastLine = GetString(_position, _length);
                    _position = _length;
                    return lastLine;
                }
                searchStart += _position;
            }
            string line = GetString(_position, lineEnd);
            _position = lineEnd + 1;
            return line;
        }
        public int ReadBytes(byte[] destination, int offset, int count)
        {
            int read = 0;
            int available;
            while (read < count)
            {
                available = _length - _position;
                if (available == 0)
                {
                    if (!FillBuffer()) break;
                    available = _length - _position;
                }
                available = Math.Min(available, count - read);
                Buffer.BlockCopy(_buffer, _position, destination, offset + read, available);
                _position += available;
                read += available;
            }
            return read;
        }
        public byte[] ReadBytes(int count)
        {
            // Null when the stream ends first
            byte[] bytes = new byte[count];
            if (ReadBytes(bytes, 0, count) < count) return null;
            return bytes;
        }
        private string GetString(int start, int end)
        {
            if (end > start && _buffer[end - 1] == '\r') end--;
            return Encoding.UTF8.GetString(_buffer, start, end - start);
        }
        private bool FillBuffer()
        {
            if (_endOfStream) return false;
            // Keep the unread bytes
            int unread = _length - _position;
            if (unread == _buffer.Length) Array.Resize(ref _buffer, _buffer.Length * 2);
            if (unread > 0 && _position > 0) Buffer.BlockCopy(_buffer, _position, _buffer, 0, unread);
            _position = 0;
            _length = unread;
            //
            int read = _stream.Read(_buffer, _length, _buffer.Length - _length);
            if (read <= 0)
            {
                _endOfStream = true;
                return false;
            }
            _length += read;
            return true;
        }
    }
}
