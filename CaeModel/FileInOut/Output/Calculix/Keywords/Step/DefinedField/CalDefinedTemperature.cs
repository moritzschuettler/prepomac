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
    internal class CalDefinedTemperature : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _regionName;
        private DefinedTemperature _definedTemperature;
        private DefinedTemperature[] _definedTemperatures;


        // Properties                                                                                                               
        public bool CanHideData { get { return _definedTemperatures != null; } }


        // Constructor                                                                                                              
        public CalDefinedTemperature(FeModel model, DefinedTemperature definedTemperature)
        {
            _definedTemperature = definedTemperature;
            _definedTemperatures = model.GetNodalTemperaturesFromVariableDefinedTemperature(_definedTemperature);
            //
            _regionName = "";
            if (_definedTemperature.RegionType == RegionTypeEnum.NodeSetName)
                _regionName += _definedTemperature.RegionName;
            else if (_definedTemperature.RegionType == RegionTypeEnum.SurfaceName)
                _regionName += model.Mesh.Surfaces[_definedTemperature.RegionName].NodeSetName;
            else if (_definedTemperature.Type == DefinedTemperatureTypeEnum.FromFile &&
                     _definedTemperature.RegionType == RegionTypeEnum.Selection)
            { }
            else throw new NotSupportedException();
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            string fileData = "";
            string amplitude = "";
            if (_definedTemperature.Type == DefinedTemperatureTypeEnum.FromFile)
                fileData = string.Format(", File={0}, Step={1}", _definedTemperature.FileName, _definedTemperature.StepNumber);
            else
            {
                if (_definedTemperature.AmplitudeName != Amplitude.DefaultAmplitudeName)
                    amplitude = ", Amplitude=" + _definedTemperature.AmplitudeName;
            }
            //
            sb.AppendLine("** Name: " + _definedTemperature.Name);
            sb.AppendFormat("*Temperature{0}{1}{2}", fileData, amplitude, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            if (_definedTemperature.Type == DefinedTemperatureTypeEnum.ByValue)
            {
                if (_definedTemperatures == null)
                {
                    sb.AppendFormat("{0}, {1}{2}", _regionName, _definedTemperature.Temperature.Value.ToCalculiX16String(),
                                    Environment.NewLine);
                }
                else
                {
                    DefinedTemperature dt;
                    for (int i = 0; i < _definedTemperatures.Length; i++)
                    {
                        dt = _definedTemperatures[i];
                        sb.AppendFormat("{0}, {1}{2}", dt.NodeId, dt.Temperature.Value.ToCalculiX16String(), Environment.NewLine);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
