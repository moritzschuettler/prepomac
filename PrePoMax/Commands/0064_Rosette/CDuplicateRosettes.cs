// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
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
    class CDuplicateRosettes : PreprocessCommand
    {
        // Variables
        private string[] _rosetteNames;


        // Constructor
        public CDuplicateRosettes(string[] rosetteNames)
            : base("Duplicate rosettes")
        {
            _rosetteNames = rosetteNames.ToArray();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateRosettes(_rosetteNames.ToArray());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + string.Join(", ", _rosetteNames);
        }
    }
}
