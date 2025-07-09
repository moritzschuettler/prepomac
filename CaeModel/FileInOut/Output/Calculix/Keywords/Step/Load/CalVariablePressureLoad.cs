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
    internal class CalVariablePressureLoad : CalLoad
    {
        // Variables                                                                                                                
        private VariablePressure _load;
        private DLoad[] _dLoads;
        private ComplexLoadTypeEnum _complexLoadType;
        private FeSurfaceFaceTypes _surfaceFaceType;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalVariablePressureLoad(FeModel model, VariablePressure load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _dLoads = model.GetElementDLoadsFromVariablePressureLoad(_load);
            _complexLoadType = complexLoadType;
            //
            _surfaceFaceType = model.Mesh.Surfaces[load.SurfaceName].SurfaceFaceTypes;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _load.Name);
            string amplitude = "";
            if (_load.AmplitudeName != Amplitude.DefaultAmplitudeName) amplitude = ", Amplitude=" + _load.AmplitudeName;
            //
            string loadCase = GetComplexLoadCase(_complexLoadType);
            //
            sb.AppendFormat("*Dload{0}{1}{2}{3}", amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            //
            if (_dLoads != null)
            {
                string faceKey = "";
                FeFaceName faceName;
                double magnitude;
                //
                foreach (var dLoad in _dLoads)
                {
                    faceName = dLoad.ElementFaceName;
                    if (_load.TwoD)
                    {
                        if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2) throw new NotSupportedException();
                        else if (faceName == FeFaceName.S3) faceKey = "P1";
                        else if (faceName == FeFaceName.S4) faceKey = "P2";
                        else if (faceName == FeFaceName.S5) faceKey = "P3";
                        else if (faceName == FeFaceName.S6) faceKey = "P4";
                    }
                    else
                    {
                        faceKey = "P" + faceName.ToString()[1];
                    }
                    //
                    magnitude = ratio * dLoad.Magnitude.Value;
                    if (_surfaceFaceType == FeSurfaceFaceTypes.ShellFaces && faceName == FeFaceName.S2) magnitude *= -1;
                    //
                    sb.AppendFormat("{0}, {1}, {2}", dLoad.ElementId, faceKey, magnitude.ToCalculiX16String()).AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
