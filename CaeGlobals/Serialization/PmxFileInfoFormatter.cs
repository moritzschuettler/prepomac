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
    // A file information is stored as the path it was made from
    public sealed class PmxFileInfoFormatter : PmxFormatter<FileInfo>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, FileInfo value, PmxWriteContext context)
        {
            if (value == null) writer.WriteNil();
            else writer.Write(value.FullName);
        }
        public override FileInfo Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return null;
            return new FileInfo(reader.ReadString());
        }
    }
}
