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
    class CAddRosette : PreprocessCommand
    {
        // Variables
        private Rosette _rosette;


        // Constructor
        public CAddRosette(Rosette rosette)
            : base("Add rosette")
        {
            _rosette = rosette.DeepCopy();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.AddRosette(_rosette.DeepCopy());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _rosette.ToString();
        }
    }
}
