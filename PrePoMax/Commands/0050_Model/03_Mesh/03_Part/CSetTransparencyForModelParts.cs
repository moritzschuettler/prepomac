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
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSetTransparencyForModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private byte _alpha;


        // Constructor                                                                                                              
        public CSetTransparencyForModelParts(string[] partNames, byte alpha)
            : base("Set transparency for model parts")
        {
            _partNames = partNames;
            _alpha = alpha;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetTransparencyForModelParts(_partNames, _alpha);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + "Alpha = " + _alpha.ToString() + ": " + GetArrayAsString(_partNames);
        }
    }
}
