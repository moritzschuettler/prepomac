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
    // The stable number a stored type is known by in a file. A file depends on this number only, never on the name of
    // the type, its namespace or its assembly, so a type may be renamed or moved freely. A number is never reused.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false,
                    AllowMultiple = false)]
    public sealed class PmxTypeAttribute : Attribute
    {
        // Variables                                                                                                                
        private readonly int _id;


        // Properties                                                                                                               
        public int Id { get { return _id; } }


        // Constructors                                                                                                             
        public PmxTypeAttribute(int id)
        {
            _id = id;
        }
    }
}
