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
    internal class AbqContactPrint : CalContactPrint
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqContactPrint(CalContactPrint calContactFile)
            : base(calContactFile)
        {
            OutputFrequency = true;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string keywordString = base.GetKeywordString();
            keywordString = keywordString.Replace(", Master=", ", Main=");
            keywordString = keywordString.Replace(", Slave=", ", Secondary=");
            return keywordString;
        }
        public override string GetDataString()
        {
            string variables = _contactHistoryOutput.Variables.ToString();
            variables = variables.Replace("CF", "CFT");
            variables = variables.Replace("CDIS", "CDISP");
            variables = variables.Replace("CSTR", "CSTRESS");
            return string.Format("{0}{1}", variables, Environment.NewLine);
        }
    }
}
