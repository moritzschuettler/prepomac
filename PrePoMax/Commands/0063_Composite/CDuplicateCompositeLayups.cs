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
    class CDuplicateCompositeLayups : PreprocessCommand
    {
        // Variables
        private string[] _compositeLayupNames;


        // Constructor
        public CDuplicateCompositeLayups(string[] compositeLayupNames)
            : base("Duplicate composite layups")
        {
            _compositeLayupNames = compositeLayupNames;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateCompositeLayups(_compositeLayupNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_compositeLayupNames);
        }
    }
}
