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
    public class CalDeactivated : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _name;


        // Properties                                                                                                               
        public string Name { get { return _name; } }


        // Constructor                                                                                                              
        public CalDeactivated(string name)
        {
            _name = name;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(("** Name: " + _name + ": Deactivated"));
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
