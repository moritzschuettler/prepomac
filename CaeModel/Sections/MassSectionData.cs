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
    [PmxType(3200)]
    public class MassSectionData : SectionData
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double _mass;               //ISerializable


        // Properties                                                                                                               
        public double Mass { get { return _mass; } set { _mass = value; } }


        // Constructors                                                                                                             
        public MassSectionData(string name, string elementSetName, double mass)
            : base(name, null, elementSetName, RegionTypeEnum.ElementSetName, 1)
        {
            _mass = mass;
        }
        public MassSectionData(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_mass":
                        _mass = (double)entry.Value;
                        break;
                    default:
                        break;
                }
            }
        }


    }
}
