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
    internal class AbqShellEdgeLoad : CalShellEdgeLoad
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqShellEdgeLoad(CalShellEdgeLoad calShellEdgeLoad)
            :base(calShellEdgeLoad)
        {
            OpType = OpTypeEnum.New;
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
            sb.AppendFormat("*Dsload{0}{1}{2}{3}", amplitude, loadCase, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            //
            string faceKey = "EDNOR";
            double ratio = GetComplexRatio(_complexLoadType, _load.PhaseDeg.Value);
            double magnitude = ratio * _load.Magnitude.Value;
            //
            sb.AppendFormat("{0}, {1}, {2}", _surface.Name, faceKey, magnitude.ToCalculiX16String()).AppendLine();
            //
            return sb.ToString();
        }
    }
}
