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
using System.Runtime.Serialization.Formatters.Binary;

namespace CaeGlobals
{
    public class BinaryObjectSerializer : IObjectSerializer
    {
        // Methods                                                                                                                  
        public T Clone<T>(T value)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
        public void Serialize(Stream stream, object value)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, value);
        }
        public object Deserialize(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Binder = RenamedTypeSerializationBinder.Instance;
            return formatter.Deserialize(stream);
        }
    }
}
