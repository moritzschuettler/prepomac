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

namespace CaeGlobals
{
    // The one place that decides how objects are cloned and written; DeepClone and the dump helpers go through it
    public static class ObjectSerializer
    {
        // Variables                                                                                                                
        private static IObjectSerializer _default = new PmxObjectSerializer();
        private static Func<Stream, object> _legacyReader = null;


        // Properties                                                                                                               
        public static IObjectSerializer Default
        {
            get { return _default; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _default = value;
            }
        }
        // Reads a stream an older version wrote with the binary formatter; the application sets it to
        // PrePoMax.LegacyImport, so that no other part of PrePoMax reads that format
        public static Func<Stream, object> LegacyReader
        {
            get { return _legacyReader; }
            set { _legacyReader = value; }
        }
    }
}
