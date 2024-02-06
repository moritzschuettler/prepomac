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
    public class MassSectionData : SectionData, ISerializable
    {
        // Variables                                                                                                                
        private double _mass;               //ISerializable


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


        // Methods                                                                                                                  
        

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_mass", _mass, typeof(double));
        }
    }
}
