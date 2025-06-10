using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;

namespace CaeModel
{
    [Serializable]
    public enum DistributionTypeEnum
    {
        [StandardValue("Scalar", DisplayName = "Scalar")]
        Scalar,
        [StandardValue("Vector", DisplayName = "Vector")]
        Vector
    }
    [Serializable]
    public abstract class Distribution : NamedClass, ISerializable
    {
        // Variables                                                                                                                
        protected DistributionTypeEnum _distributionType;           //ISerializable


        // Properties                                                                                                               
        public DistributionTypeEnum DistributionType { get { return _distributionType; } set { _distributionType = value; } }
        public int NumOfComponents
        {
            get
            {
                if (_distributionType == DistributionTypeEnum.Scalar) return 1;
                else if (_distributionType == DistributionTypeEnum.Vector) return 3;
                else throw new NotSupportedException();
            }
        }


        // Constructors                                                                                                             
        public Distribution(string name)
            : base(name)
        {
            _distributionType = DistributionTypeEnum.Scalar;
        }
        public Distribution(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_distributionType":
                        _distributionType = (DistributionTypeEnum)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public abstract bool IsInitialized();
        public abstract bool ImportDistribution();
        public abstract void GetMagnitudeAndDistanceForPoint(double[] point, out double[] magnitude, out double[] distance);
        public abstract void GetMagnitudesAndDistancesForPoints(double[][] points, out double[][] magnitudes,
                                                                out double[][] distances);
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_distributionType", _distributionType, typeof(DistributionTypeEnum));
        }
    }
}
