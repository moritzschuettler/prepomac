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
    internal class CalFriction : CalculixKeyword
    {
        // Variables                                                                                                                
        private Friction _friction;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalFriction(Friction friction)
        {
            _friction = friction;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Friction{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_friction.Coefficient.ToCalculiX16String());
            if (!double.IsNaN(_friction.StickSlope)) sb.AppendFormat(", {0}", _friction.StickSlope.ToCalculiX16String());
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
