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
    class CReplaceCompositeLayup : PreprocessCommand
    {
        // Variables
        private string _oldCompositeLayupName;
        private CompositeLayup _newCompositeLayup;


        // Constructor
        public CReplaceCompositeLayup(string oldCompositeLayupName, CompositeLayup newCompositeLayup)
            : base("Edit composite layup")
        {
            _oldCompositeLayupName = oldCompositeLayupName;
            _newCompositeLayup = newCompositeLayup.DeepClone();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceCompositeLayup(_oldCompositeLayupName, _newCompositeLayup.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldCompositeLayupName + ", " + _newCompositeLayup.ToString();
        }
    }
}
