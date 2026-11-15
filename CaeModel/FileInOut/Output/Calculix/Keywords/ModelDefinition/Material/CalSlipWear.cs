// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(3094)]
    internal class CalSlipWear : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] private SlipWear _slipWear;
        [PmxKey(1)] private bool _temperatureDependent;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSlipWear(SlipWear slipWear, bool temperatureDependent)
        {
            _slipWear = slipWear;
            _temperatureDependent = temperatureDependent;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Slip wear{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}, {1}{2}", _slipWear.Hardness, _slipWear.WearCoefficient, Environment.NewLine);
        }
    }
}
