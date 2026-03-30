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

namespace PrePoMax.Commands
{
    [Serializable]
    class CRemoveBCs : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string[] _boundaryConditionNames;

        // Constructor                                                                                                              
        public CRemoveBCs(string stepName, string[] boundaryConditionNames)
            :base("Remove BCs")
        {
            _stepName = stepName;
            _boundaryConditionNames = boundaryConditionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveBoundaryConditions(_stepName, _boundaryConditionNames);
            return true;
        }

        public override string GetCommandString()
        {

            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_boundaryConditionNames);
        }
    }
}
