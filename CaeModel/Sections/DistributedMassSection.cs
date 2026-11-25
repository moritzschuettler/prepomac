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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(3196)]
    public class DistributedMassSection : MassSection
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public DistributedMassSection(string name, string regionName, RegionTypeEnum regionType, double mass, bool twoD)
            : base(name, regionName, regionType, mass, twoD)
        {
        }
        public DistributedMassSection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


    }
}