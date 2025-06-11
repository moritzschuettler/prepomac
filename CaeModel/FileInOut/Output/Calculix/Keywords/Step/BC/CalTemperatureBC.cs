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
    internal class CalTemperatureBC : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _nodeSetNameOfSurface;
        private TemperatureBC _temperatureBC;
        private TemperatureBC[] _temperatureBCs;


        // Properties                                                                                                               
        public bool CanHideData { get { return _temperatureBCs != null; } }


        // Constructor                                                                                                              
        public CalTemperatureBC(FeModel model, TemperatureBC temperatureBC, string nodeSetNameOfSurface)
        {
            _temperatureBC = temperatureBC;
            _temperatureBCs = model.GetNodalTemperaturesFromVariableTemperatureBC(temperatureBC);
            _nodeSetNameOfSurface = nodeSetNameOfSurface;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _temperatureBC.Name);
            string amplitude = "";
            if (_temperatureBC.AmplitudeName != Amplitude.DefaultAmplitudeName)
                amplitude = ", Amplitude=" + _temperatureBC.AmplitudeName;
            //
            sb.AppendFormat("*Boundary{0}{1}", amplitude, Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            // *Boundary
            // 6975, 11, 11, 100        node id, start DOF, end DOF, value
            // Node set
            if (_temperatureBCs == null)
            {
                string regionName;
                if (_temperatureBC.RegionType == RegionTypeEnum.NodeSetName)
                {
                    regionName = _temperatureBC.RegionName;
                }
                // Surface
                else if (_temperatureBC.RegionType == RegionTypeEnum.SurfaceName)
                {
                    if (_nodeSetNameOfSurface == null) throw new ArgumentException();
                    regionName = _nodeSetNameOfSurface;
                }
                else throw new NotSupportedException();
                //
                sb.AppendFormat("{0}, 11, 11, {1}{2}", regionName, _temperatureBC.Temperature.Value.ToCalculiX16String(),
                                Environment.NewLine);
            }
            else
            {
                TemperatureBC tbc;
                for (int i = 0; i < _temperatureBCs.Length; i++)
                {
                    tbc = _temperatureBCs[i];
                    sb.AppendFormat("{0}, 11, 11, {1}{2}", tbc.NodeId, tbc.Temperature.Value.ToCalculiX16String(),
                                    Environment.NewLine);
                }
            }
            //
            return sb.ToString();
        }
    }
}
