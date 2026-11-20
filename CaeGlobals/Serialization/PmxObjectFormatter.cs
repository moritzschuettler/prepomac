// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using MessagePack;

namespace CaeGlobals
{
    // A slot that may hold an object of more than one type: a [PmxType] class, an object, an interface or an abstract or
    // open class. It stores the type of the value it holds:
    //   nil                                     null
    //   extension 1                             an object written before
    //   [type, reference number, {key: value}]  a [PmxType] class
    //   [type, value]                           any other value, written by the formatter of its type
    public sealed class PmxObjectFormatter<T> : PmxFormatter<T>
    {
        // Methods                                                                                                                  
        public override void Write(ref MessagePackWriter writer, T value, PmxWriteContext context)
        {
            object item = value;
            if (item == null || item is Delegate)
            {
                writer.WriteNil();
                return;
            }
            Type type = item.GetType();
            bool isReferenceType = !type.IsValueType && !(item is string) && !(item is Type);
            if (isReferenceType && context.TryWriteExistingReference(ref writer, item)) return;
            //
            PmxClassPlan plan = type.IsValueType ? null : PmxClassPlan.TryGet(type);
            if (plan != null)
            {
                writer.WriteArrayHeader(3);
                plan.WriteTypeReference(ref writer);
                writer.Write(context.AddReference(item));
                plan.WriteBody(ref writer, item, context);
            }
            else
            {
                IPmxFormatter formatter = PmxFormatterResolver.GetBoxed(item is Type ? typeof(Type) : type);
                if (formatter is PmxObjectFormatter<object> || formatter.GetType().IsGenericType &&
                    formatter.GetType().GetGenericTypeDefinition() == typeof(PmxObjectFormatter<>))
                    throw new PmxStorageException("The type " + type.FullName + " has no storage schema.");
                writer.WriteArrayHeader(2);
                PmxTypeCodec.Write(ref writer, item is Type ? typeof(Type) : type);
                formatter.WriteObject(ref writer, item, context);
            }
        }
        public override T Read(ref MessagePackReader reader, PmxReadContext context)
        {
            if (reader.TryReadNil()) return default(T);
            object existing;
            if (context.TryReadReference(ref reader, out existing)) return (T)existing;
            //
            int count = reader.ReadArrayHeader();
            Type type = PmxTypeCodec.Read(ref reader);
            if (count == 3)
            {
                PmxClassPlan plan = PmxClassPlan.Get(type);
                int referenceId = reader.ReadInt32();
                object item = plan.CreateUninitialized();
                context.AddReference(referenceId, item);
                plan.ReadBody(ref reader, item, context);
                context.Completed(item, plan);
                return (T)item;
            }
            if (count != 2) throw new PmxStorageException("The file holds a damaged object of the type " + type.FullName + ".");
            return (T)PmxFormatterResolver.GetBoxed(type).ReadObject(ref reader, context);
        }
    }
}
