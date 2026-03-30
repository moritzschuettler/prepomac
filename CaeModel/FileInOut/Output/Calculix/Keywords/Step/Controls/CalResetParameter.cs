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

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalResetParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private ResetStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalResetParameter(ResetStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Reset{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
