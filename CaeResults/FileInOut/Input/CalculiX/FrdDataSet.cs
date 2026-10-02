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

namespace CaeResults
{
    internal class FrdDataSet
    {
        // Variables
        private string[] _lines;
        private int _binaryFormat;
        private byte[] _binaryData;


        // Properties
        public string[] Lines { get { return _lines; } }
        public bool IsBinary { get { return _binaryData != null; } }
        public int BinaryFormat { get { return _binaryFormat; } }
        public byte[] BinaryData { get { return _binaryData; } }


        // Constructors
        public FrdDataSet(string[] lines)
            : this(lines, 0, null)
        {
        }
        public FrdDataSet(string[] lines, int binaryFormat, byte[] binaryData)
        {
            _lines = lines;
            _binaryFormat = binaryFormat;
            _binaryData = binaryData;
        }
    }
}
