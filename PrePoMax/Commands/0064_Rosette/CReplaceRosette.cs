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
    class CReplaceRosette : PreprocessCommand
    {
        // Variables
        private string _oldRosetteName;
        private Rosette _newRosette;


        // Constructor
        public CReplaceRosette(string oldRosetteName, Rosette newRosette)
            : base("Edit rosette")
        {
            _oldRosetteName = oldRosetteName;
            _newRosette = newRosette.DeepCopy();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceRosette(_oldRosetteName, _newRosette.DeepCopy());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldRosetteName + ", " + _newRosette.ToString();
        }
    }
}
