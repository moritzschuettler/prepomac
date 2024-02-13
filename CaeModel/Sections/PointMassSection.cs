using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class PointMassSection : MassSection, ISerializable
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public PointMassSection(string name, string regionName, RegionTypeEnum regionType, double mass, bool twoD)
            : base(name, regionName, regionType, mass, twoD)
        {
        }
        public PointMassSection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
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