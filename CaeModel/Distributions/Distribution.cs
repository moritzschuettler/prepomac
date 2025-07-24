using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CaeGlobals;
using CaeMesh;
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
        protected string _coordinateSystemName;                     //ISerializable
        public const string DefaultDistributionName = "Constant";


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
        public string CoordinateSystemName
        {
            get
            {
                if (_coordinateSystemName == null) return CoordinateSystem.DefaultCoordinateSystemName;
                else return _coordinateSystemName;
            }
            set
            {
                _coordinateSystemName = value;
                if (_coordinateSystemName == CoordinateSystem.DefaultCoordinateSystemName) _coordinateSystemName = null;
            }
        }


        // Constructors                                                                                                             
        public Distribution(string name)
            : base(name)
        {
            _distributionType = DistributionTypeEnum.Scalar;
            _coordinateSystemName = null;
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
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public abstract bool ImportDistribution();
        public abstract void GetMagnitudesAndDistancesForPoints(FeModel model, double[][] points, out double[][] magnitudes,
                                                                out double[][] distances);
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_distributionType", _distributionType, typeof(DistributionTypeEnum));
            info.AddValue("_coordinateSystemName", _coordinateSystemName, typeof(string));
        }
    }
}
