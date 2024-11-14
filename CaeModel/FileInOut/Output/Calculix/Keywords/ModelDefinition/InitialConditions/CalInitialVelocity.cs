using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalInitialVelocity : CalculixKeyword
    {
        // Variables                                                                                                                
        private InitialVelocity _initialVelocity;
        private Dictionary<string, int[]> _referencePointsNodeIds;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalInitialVelocity(InitialVelocity initialVelocity, Dictionary<string, int[]> referencePointsNodeIds)
        {
            _initialVelocity = initialVelocity;
            _referencePointsNodeIds = referencePointsNodeIds;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _initialVelocity.Name);
            sb.AppendLine("*Initial conditions, Type=Velocity");
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            int[] rpNodeIds = null;
            if (_initialVelocity.RegionType == RegionTypeEnum.ReferencePointName)
                rpNodeIds = _referencePointsNodeIds[_initialVelocity.RegionName];
            //
            string region;
            if (_initialVelocity.RegionType == RegionTypeEnum.NodeSetName) region = _initialVelocity.RegionName;
            else if (_initialVelocity.RegionType == RegionTypeEnum.ReferencePointName) region = rpNodeIds[0].ToString();
            else throw new NotSupportedException();
            //
            if (_initialVelocity.V1 != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", region, 1, _initialVelocity.V1.ToCalculiX16String(),
                                Environment.NewLine);
            if (_initialVelocity.V2 != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", region, 2, _initialVelocity.V2.ToCalculiX16String(),
                                Environment.NewLine);
            if (_initialVelocity.V3 != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", region, 3, _initialVelocity.V3.ToCalculiX16String(),
                                Environment.NewLine);
            return sb.ToString();
        }
    }
}
