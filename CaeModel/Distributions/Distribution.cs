using System;
using System.Collections.Generic;
using System.Text;
using CaeGlobals;
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
    public abstract class Distribution : NamedClass
    {
        // Variables                                                                                                                
        protected DistributionTypeEnum _distributionType;


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
            Reset();
        }


        // Methods                                                                                                                  
        private void Reset()
        {
            _distributionType = DistributionTypeEnum.Scalar;
        }
        public abstract bool IsInitialized();
        public abstract bool ImportLoad();
        public abstract double[] GetMagnitudeForPoint(double[] point);
        public abstract double[][] GetMagnitudesForPoints(double[][] point);
    }
}
