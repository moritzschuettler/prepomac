using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using System.Drawing;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalInitialTranslationalVelocity : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _regionName;
        private InitialTranslationalVelocity _initialVelocity;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalInitialTranslationalVelocity(FeModel model, InitialTranslationalVelocity initialVelocity,
                                               Dictionary<string, int[]> referencePointsNodeIds)
        {
            _initialVelocity = initialVelocity;
            //
            if (_initialVelocity.RegionType == RegionTypeEnum.NodeId)
                _regionName = initialVelocity.NodeId.ToString();
            else if (_initialVelocity.RegionType == RegionTypeEnum.NodeSetName)
                _regionName = _initialVelocity.RegionName;
            else if (_initialVelocity.RegionType == RegionTypeEnum.SurfaceName)
                _regionName += model.Mesh.Surfaces[_initialVelocity.RegionName].NodeSetName;
            else if (_initialVelocity.RegionType == RegionTypeEnum.ReferencePointName)
            {
                int[] rpNodeIds = referencePointsNodeIds[_initialVelocity.RegionName];
                _regionName = rpNodeIds[0].ToString();
            }
            else throw new NotSupportedException();
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
            if (_initialVelocity.V1.Value != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", _regionName, 1, _initialVelocity.V1.Value.ToCalculiX16String(),
                                Environment.NewLine);
            if (_initialVelocity.V2.Value != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", _regionName, 2, _initialVelocity.V2.Value.ToCalculiX16String(),
                                Environment.NewLine);
            if (_initialVelocity.V3.Value != 0)
                sb.AppendFormat("{0}, {1}, {2}{3}", _regionName, 3, _initialVelocity.V3.Value.ToCalculiX16String(),
                                Environment.NewLine);
            return sb.ToString();
        }
    }
}
