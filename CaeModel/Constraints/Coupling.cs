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
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;


namespace CaeModel
{
    [Serializable]
    public class Coupling : Constraint, IMultiRegion, ISerializable // ISerializable must be here
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public string ReferencePointName { get { return MasterRegionName; } set { MasterRegionName = value; } }
        public string RegionName { get { return SlaveRegionName; } set { SlaveRegionName = value; } }
        public RegionTypeEnum RegionType { get { return SlaveRegionType; } set { SlaveRegionType = value; } }
        public int[] CreationIds { get { return SlaveCreationIds; } set { SlaveCreationIds = value; } }
        public Selection CreationData { get { return SlaveCreationData; } set { SlaveCreationData = value; } }


        // Constructors                                                                                                             
        public Coupling(string name, string referencePointName, string regionName, RegionTypeEnum regionType, bool twoD)
            : base(name, referencePointName, RegionTypeEnum.ReferencePointName, regionName, regionType, twoD)
        {
        }
        public Coupling(SerializationInfo info, StreamingContext context)
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
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
        }
    }
}
