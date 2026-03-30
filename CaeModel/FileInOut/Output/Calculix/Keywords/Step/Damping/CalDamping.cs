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
    internal class CalDamping : CalculixKeyword
    {
        // Variables                                                                                                                
        private Damping _damping;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalDamping(Damping damping)
        {
            if (damping.DampingType == DampingTypeEnum.Off)
                throw new NotSupportedException();
            //
            _damping = damping;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Damping, Alpha={0}, Beta={1}{2}", _damping.Alpha.ToCalculiX16String(),
                                                                     _damping.Beta.ToCalculiX16String(),
                                                                     Environment.NewLine);
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
