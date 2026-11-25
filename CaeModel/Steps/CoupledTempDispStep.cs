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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using CaeGlobals;


namespace CaeModel
{
    [PmxType(3222)]
    public class CoupledTempDispStep : UncoupledTempDispStep
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


    }
}
