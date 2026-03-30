// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace CaeModel
{
    [Serializable]
    public class CoupledTempDispStep : UncoupledTempDispStep, ISerializable
    {
        // Variables                                                                                                                
        

        // Properties                                                                                                               


        // Constructors                                                                                                             
        public CoupledTempDispStep(string name)
            :base(name)
        {
        }
        //ISerializable
        public CoupledTempDispStep(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
       
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
