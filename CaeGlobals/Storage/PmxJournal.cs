// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CaeGlobals
{
    // An append-only list of items in a file, one frame per item, so that recording an item writes one frame instead of
    // the whole list and a crash loses at most the frame being written:
    //   "PMH2", int32 version, then frames: int32 payload length, uint32 CRC-32 of the payload, payload
    // A payload is a storage payload of one item. A reader stops at the first frame that is cut off or damaged.
    public sealed class PmxJournal
    {
        // Variables                                                                                                                
        public const int JournalVersion = 1;
        private static readonly byte[] _magic = Encoding.ASCII.GetBytes("PMH2");
        private readonly string _fileName;
        private readonly PmxObjectSerializer _serializer;
        private readonly List<object> _items;
        private readonly List<long> _frameOffsets;
        private byte[] _lastPayload;
        private long _length;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } }


        // Constructors                                                                                                             
        public PmxJournal(string fileName)
        {
            _fileName = fileName;
            _serializer = new PmxObjectSerializer();
            _items = new List<object>();
            _frameOffsets = new List<long>();
            _lastPayload = null;
            _length = -1;
        }


        // Static methods                                                                                                           
        public static bool IsJournal(string fileName)
        {
            byte[] start = new byte[_magic.Length];
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Read(start, 0, start.Length) != start.Length) return false;
            }
            for (int i = 0; i < _magic.Length; i++)
            {
                if (start[i] != _magic[i]) return false;
            }
            return true;
        }
        public static List<object> Read(string fileName, out bool isComplete)
        {
            List<object> items = new List<object>();
            PmxObjectSerializer serializer = new PmxObjectSerializer();
            byte[] bytes = File.ReadAllBytes(fileName);
            isComplete = true;
            if (bytes.Length < 8) throw new PmxStorageException("The history file is damaged.");
            for (int i = 0; i < _magic.Length; i++)
            {
                if (bytes[i] != _magic[i]) throw new PmxStorageException("The file is not a PrePoMax history journal.");
            }
            if (BitConverter.ToInt32(bytes, 4) > JournalVersion)
                throw new PmxStorageException("The history file was written by a newer version of PrePoMax.");
            //
            int position = 8;
            while (position < bytes.Length)
            {
                if (position + 8 > bytes.Length)
                {
                    isComplete = false;
                    break;
                }
                int length = BitConverter.ToInt32(bytes, position);
                uint crc32 = BitConverter.ToUInt32(bytes, position + 4);
                if (length < 0 || position + 8 + length > bytes.Length || PmxCrc32.Compute(bytes, position + 8, length) != crc32)
                {
                    isComplete = false;
                    break;
                }
                items.Add(serializer.DeserializeFromBytes(bytes, position + 8, length));
                position += 8 + length;
            }
            return items;
        }
        public static void WriteAll(string fileName, IList items)
        {
            new PmxJournal(fileName).Write(items);
        }


        // Methods                                                                                                                  
        // Brings the file in line with the items: the frames of the items that are unchanged at the start stay, the rest
        // is written anew. The last item written before is serialized again, since an item may change after it was
        // recorded, as a save command does when it stores the hash of the file it wrote.
        public void Write(IList items)
        {
            int unchanged = 0;
            while (unchanged < _items.Count && unchanged < items.Count && ReferenceEquals(_items[unchanged], items[unchanged]))
                unchanged++;
            byte[] lastPayload = null;
            if (unchanged > 0 && unchanged == _items.Count)
            {
                lastPayload = _serializer.SerializeToBytes(items[unchanged - 1]);
                if (!AreEqual(lastPayload, _lastPayload)) unchanged--;
                else lastPayload = null;
            }
            //
            using (FileStream stream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                if (_length < 0 || stream.Length != _length)
                {
                    // A new journal, or a file another session changed: write it whole
                    unchanged = 0;
                    lastPayload = null;
                }
                long offset = unchanged < _frameOffsets.Count ? _frameOffsets[unchanged] : _length;
                if (unchanged == 0)
                {
                    stream.SetLength(0);
                    stream.Write(_magic, 0, _magic.Length);
                    stream.Write(BitConverter.GetBytes(JournalVersion), 0, 4);
                    offset = stream.Position;
                }
                else stream.SetLength(offset);
                stream.Position = offset;
                //
                _items.RemoveRange(unchanged, _items.Count - unchanged);
                _frameOffsets.RemoveRange(unchanged, _frameOffsets.Count - unchanged);
                for (int i = unchanged; i < items.Count; i++)
                {
                    byte[] payload = i == unchanged && lastPayload != null ? lastPayload : _serializer.SerializeToBytes(items[i]);
                    _frameOffsets.Add(stream.Position);
                    stream.Write(BitConverter.GetBytes(payload.Length), 0, 4);
                    stream.Write(BitConverter.GetBytes(PmxCrc32.Compute(payload)), 0, 4);
                    stream.Write(payload, 0, payload.Length);
                    _items.Add(items[i]);
                    _lastPayload = payload;
                }
                if (items.Count == 0) _lastPayload = null;
                stream.Flush();
                _length = stream.Length;
            }
        }
        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
