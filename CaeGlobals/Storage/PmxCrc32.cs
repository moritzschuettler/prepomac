// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeGlobals
{
    // The CRC-32 of zlib and PNG (polynomial 0xEDB88320, reflected), which detects a damaged chunk or journal frame.
    // Computed eight bytes at a time with eight tables, since the result chunks are tens of megabytes.
    public static class PmxCrc32
    {
        // Variables                                                                                                                
        private static readonly uint[] _tables = CreateTables();


        // Methods                                                                                                                  
        private static uint[] CreateTables()
        {
            uint[] tables = new uint[8 * 256];
            for (uint i = 0; i < 256; i++)
            {
                uint value = i;
                for (int bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
                tables[i] = value;
            }
            for (int t = 1; t < 8; t++)
            {
                for (int i = 0; i < 256; i++)
                {
                    uint previous = tables[(t - 1) * 256 + i];
                    tables[t * 256 + i] = tables[previous & 0xFF] ^ (previous >> 8);
                }
            }
            return tables;
        }
        public static uint Compute(byte[] bytes)
        {
            return Compute(bytes, 0, bytes.Length);
        }
        public static uint Compute(byte[] bytes, int offset, int count)
        {
            uint[] t = _tables;
            uint crc = 0xFFFFFFFFu;
            int i = offset;
            int end = offset + count;
            for (; i + 8 <= end; i += 8)
            {
                uint low = crc ^ (uint)(bytes[i] | bytes[i + 1] << 8 | bytes[i + 2] << 16 | bytes[i + 3] << 24);
                uint high = (uint)(bytes[i + 4] | bytes[i + 5] << 8 | bytes[i + 6] << 16 | bytes[i + 7] << 24);
                crc = t[7 * 256 + (low & 0xFF)] ^ t[6 * 256 + ((low >> 8) & 0xFF)] ^
                      t[5 * 256 + ((low >> 16) & 0xFF)] ^ t[4 * 256 + (low >> 24)] ^
                      t[3 * 256 + (high & 0xFF)] ^ t[2 * 256 + ((high >> 8) & 0xFF)] ^
                      t[256 + ((high >> 16) & 0xFF)] ^ t[high >> 24];
            }
            for (; i < end; i++) crc = t[(crc ^ bytes[i]) & 0xFF] ^ (crc >> 8);
            return ~crc;
        }
    }
}
