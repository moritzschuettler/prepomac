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
using System.Reflection.Emit;
using MessagePack;

namespace CaeGlobals
{
    public sealed class PmxTypedMember<TField> : PmxMember
    {
        // Variables                                                                                                                
        private readonly Func<object, TField> _getter;
        private readonly Action<object, TField> _setter;


        // Constructors                                                                                                             
        private PmxTypedMember(FieldInfo field, int combinedKey)
            : base(field, combinedKey)
        {
            _getter = CreateGetter(field);
            _setter = CreateSetter(field);
        }


        // Methods                                                                                                                  
        private static Func<object, TField> CreateGetter(FieldInfo field)
        {
            DynamicMethod method = new DynamicMethod("get_" + field.Name, typeof(TField), new Type[] { typeof(object) },
                                                     typeof(PmxMember).Module, true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (field.DeclaringType.IsValueType) il.Emit(OpCodes.Unbox, field.DeclaringType);
            else il.Emit(OpCodes.Castclass, field.DeclaringType);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<object, TField>)method.CreateDelegate(typeof(Func<object, TField>));
        }
        private static Action<object, TField> CreateSetter(FieldInfo field)
        {
            DynamicMethod method = new DynamicMethod("set_" + field.Name, null, new Type[] { typeof(object), typeof(TField) },
                                                     typeof(PmxMember).Module, true);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (field.DeclaringType.IsValueType) il.Emit(OpCodes.Unbox, field.DeclaringType);
            else il.Emit(OpCodes.Castclass, field.DeclaringType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<object, TField>)method.CreateDelegate(typeof(Action<object, TField>));
        }
        public override void Write(ref MessagePackWriter writer, object owner, PmxWriteContext context)
        {
            PmxFormatterCache<TField>.Formatter.Write(ref writer, _getter(owner), context);
        }
        public override void Read(ref MessagePackReader reader, object owner, PmxReadContext context)
        {
            _setter(owner, PmxFormatterCache<TField>.Formatter.Read(ref reader, context));
        }
    }
}
