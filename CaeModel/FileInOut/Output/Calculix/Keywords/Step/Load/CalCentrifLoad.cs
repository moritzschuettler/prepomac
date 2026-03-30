// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    internal class CalCentrifLoad : CalLoad
    {
        // Variables                                                                                                                
        private CentrifLoad _load;
        private ComplexLoadTypeEnum _complexLoadType;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalCentrifLoad(CentrifLoad load, ComplexLoadTypeEnum complexLoadType)
        {
            _load = load;
            _complexLoadType = complexLoadType;
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
            Vec3D n = ratio * new Vec3D(_load.N1.Value, _load.N2.Value, _load.N3.Value);
            n.Normalize();
            //
            sb.AppendFormat("{0}, CENTRIF, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                            _load.RegionName,
                            _load.RotationalSpeed2.ToCalculiX16String(), 
                            _load.X.Value.ToCalculiX16String(),
                            _load.Y.Value.ToCalculiX16String(),
                            _load.Z.Value.ToCalculiX16String(),
                            n.X.ToCalculiX16String(),
                            n.Y.ToCalculiX16String(),
                            n.Z.ToCalculiX16String());
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
