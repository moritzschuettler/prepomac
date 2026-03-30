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
    class CDuplicateMeshRefinements : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _meshRefinementNames;


        // Constructor                                                                                                              
        public CDuplicateMeshRefinements(string[] meshRefinementNames)
            : base("Duplicate mesh refinements")
        {
            _meshRefinementNames = meshRefinementNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateMeshSetupItems(_meshRefinementNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_meshRefinementNames);
        }
    }
}
