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
    public class PmxStorageException : Exception
    {
        // Constructors                                                                                                             
        public PmxStorageException(string message)
            : base(message)
        {
        }
        public PmxStorageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
