// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CaeGlobals
{
    public sealed class PmxReferenceComparer : IEqualityComparer<object>
    {
        // Variables                                                                                                                
        public static readonly PmxReferenceComparer Instance = new PmxReferenceComparer();


        // Methods                                                                                                                  
        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
