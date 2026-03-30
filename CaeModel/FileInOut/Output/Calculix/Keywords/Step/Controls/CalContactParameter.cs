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
    internal class CalContactParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private ContactStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalContactParameter(ContactStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Parameters=Contact{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, {1}, {2}, {3}{4}",
                            _parameter.Delcon.ToCalculiX16String(), _parameter.Alea.ToCalculiX16String(),
                            _parameter.Kscalemax, _parameter.Itf2f, Environment.NewLine);
            return sb.ToString();
        }
    }
}
