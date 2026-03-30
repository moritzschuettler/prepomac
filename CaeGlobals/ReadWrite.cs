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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    public static class ReadWrite
    {
        // Int[]
        public static void WriteToBinaryStream(int[] data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(data.Length);
                for (int i = 0; i < data.Length; i++) bw.Write(data[i]);
            }
        }
        public static void ReadFromBinaryStream(out int[] data, BinaryReader br)
        {
            int numOfEntries = br.ReadInt32();
            if (numOfEntries <= -1) data = null;
            else
            {
                data = new int[numOfEntries];
                for (int i = 0; i < data.Length; i++) data[i] = br.ReadInt32();
            }
        }
        // Double[]
        public static void WriteToBinaryStream(double[] data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(data.Length);
                for (int i = 0; i < data.Length; i++) bw.Write(data[i]);
            }
        }
        public static void ReadFromBinaryStream(out double[] data, BinaryReader br)
        {
            int numOfEntries = br.ReadInt32();
            if (numOfEntries <= -1) data = null;
            else
            {
                data = new double[numOfEntries];
                for (int i = 0; i < data.Length; i++) data[i] = br.ReadDouble();
            }
        }
        // Int[][]
        public static void WriteToBinaryStream(this int[][] data, BinaryWriter bw)
        {
            if (data == null)
            {
                bw.Write(-1);
            }
            else
            {
                bw.Write(data.Length);
                for (int i = 0; i < data.Length; i++) WriteToBinaryStream(data[i], bw);
            }
        }
        public static void ReadFromBinaryStream(out int[][] data, BinaryReader br)
        {
            int numOfEntries = br.ReadInt32();
            if (numOfEntries <= -1) data = null;
            else
            {
                data = new int[numOfEntries][];
                for (int i = 0; i < data.Length; i++) ReadFromBinaryStream(out data[i], br);
            }
        }
    }
}
