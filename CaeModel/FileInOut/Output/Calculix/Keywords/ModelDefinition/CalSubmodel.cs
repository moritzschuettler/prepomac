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
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalSubmodel : CalculixKeyword
    {
        // Variables                                                                                                                
        string _globalResultsFileName;
        string[] _nodeSetNames;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalSubmodel(string globalResultsFileName, string[] nodeSetNames)
        {
            _globalResultsFileName = globalResultsFileName;
            _nodeSetNames = nodeSetNames;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            if (_globalResultsFileName != null)
                return string.Format("*Submodel, Type=Node, Input=\"{0}\"{1}", _globalResultsFileName.ToUTF8(), Environment.NewLine);
            else
                throw new CaeException("Submodel BC: the file with the global result is not defined (Model -> Edit)");
        }

        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var nodeSetName in _nodeSetNames) sb.AppendLine(nodeSetName);
            return sb.ToString();
        }
    }
}
