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
    // The stable number a stored field is known by within the type that declares it. A field may be renamed freely; a
    // number is never reused for a field of another meaning. An auto-property takes it as [field: PmxKey(n)].
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class PmxKeyAttribute : Attribute
    {
        // Variables                                                                                                                
        public const int MaxKey = 1023;
        private readonly int _key;


        // Properties                                                                                                               
        public int Key { get { return _key; } }


        // Constructors                                                                                                             
        public PmxKeyAttribute(int key)
        {
            if (key < 0 || key > MaxKey) throw new ArgumentOutOfRangeException(nameof(key));
            _key = key;
        }
    }
}
