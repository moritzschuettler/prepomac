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
    class CReplaceInitialCondition : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldInitialConditionName;
        private InitialCondition _newInitialCondition;


        // Constructor                                                                                                              
        public CReplaceInitialCondition(string oldInitialConditionName, InitialCondition newInitialCondition)
            : base("Edit initial condition")
        {
            _oldInitialConditionName = oldInitialConditionName;
            _newInitialCondition = newInitialCondition.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceInitialCondition(_oldInitialConditionName, _newInitialCondition.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldInitialConditionName + ", " + _newInitialCondition.ToString();
        }
    }
}
