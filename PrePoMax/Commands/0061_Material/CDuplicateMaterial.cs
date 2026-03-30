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
    class CDuplicateMaterial : PreprocessCommand
    {
        // Compatibility v1.4.0 - the name of the class is missing one s - CDuplicateMaterials

        // Variables                                                                                                                
        private string[] _materialNames;


        // Constructor                                                                                                              
        public CDuplicateMaterial(string[] materialNames)
            : base("Duplicate materials")
        {
            _materialNames = materialNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateMaterials(_materialNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_materialNames);
        }
    }
}
