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
    internal class CalBodyFlux : CalLoad
    {
        // Variables                                                                                                                
        private BodyFlux _flux;

        
        // Constructor                                                                                                              
        public CalBodyFlux(BodyFlux flux)
        {
            _flux = flux;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("** Name: " + _flux.Name);
            string amplitude = "";
            if (_flux.AmplitudeName != Amplitude.DefaultAmplitudeName) amplitude = ", Amplitude=" + _flux.AmplitudeName;
            //
            sb.AppendFormat("*Dflux{0}{1}{2}", amplitude, OpTypeString(), Environment.NewLine);
            //
            return sb.ToString();
        }
        public override string GetDataString()
        {
            //*Dflux
            //ElementSet-1, BF, 10
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, BF, {1}{2}", _flux.RegionName, _flux.Magnitude.Value.ToCalculiX16String(), Environment.NewLine);
            return sb.ToString();
        }
    }
}
