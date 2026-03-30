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
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceBC : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _oldBoundaryConditionName;
        private BoundaryCondition _newBoundaryCondition;


        // Constructor                                                                                                              
        public CReplaceBC(string stepName, string oldBoundaryConditionName, BoundaryCondition newBoundaryCondition)
            : base("Edit BC")
        {
            _stepName = stepName;
            _oldBoundaryConditionName = oldBoundaryConditionName;
            _newBoundaryCondition = newBoundaryCondition.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceBoundaryCondition(_stepName, _oldBoundaryConditionName, _newBoundaryCondition.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _oldBoundaryConditionName + ", " + _newBoundaryCondition.ToString();
        }
    }
}
