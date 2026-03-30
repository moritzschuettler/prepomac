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
    class CDuplicateContactPairs : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _contactPairNames;


        // Constructor                                                                                                              
        public CDuplicateContactPairs(string[] contactPairNames)
            : base("Duplicate contact pairs")
        {
            _contactPairNames = contactPairNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateContactPairs(_contactPairNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_contactPairNames);
        }
    }
}
