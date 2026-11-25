// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Reflection;
using MessagePack;

namespace CaeGlobals
{
    // One stored field: reads and writes it through methods emitted for it, which also reach a read only field
    public abstract class PmxMember
    {
        // Variables                                                                                                                
        private readonly FieldInfo _field;
        private readonly int _combinedKey;


        // Properties                                                                                                               
        public FieldInfo Field { get { return _field; } }
        public int CombinedKey { get { return _combinedKey; } }


        // Constructors                                                                                                             
        protected PmxMember(FieldInfo field, int combinedKey)
        {
            _field = field;
            _combinedKey = combinedKey;
        }


        // Methods                                                                                                                  
        public static PmxMember Create(FieldInfo field, int combinedKey)
        {
            Type memberType = typeof(PmxTypedMember<>).MakeGenericType(field.FieldType);
            return (PmxMember)Activator.CreateInstance(memberType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                       new object[] { field, combinedKey }, null);
        }
        public abstract void Write(ref MessagePackWriter writer, object owner, PmxWriteContext context);
        public abstract void Read(ref MessagePackReader reader, object owner, PmxReadContext context);
    }
}
