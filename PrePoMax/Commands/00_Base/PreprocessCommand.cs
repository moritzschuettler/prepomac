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
using System.Reflection;
using CaeGlobals;
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    public abstract class PreprocessCommand : Command
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public PreprocessCommand(string name)
            : base(name)
        {
        }
        protected PreprocessCommand(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        // Methods                                                                                                                  
        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
