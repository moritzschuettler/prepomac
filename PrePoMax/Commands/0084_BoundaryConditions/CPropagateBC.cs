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
    class CPropagateBC : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _boundaryConditionName;


        // Constructor                                                                                                              
        public CPropagateBC(string stepName, string boundaryConditionName)
            : base("Propagate BC")
        {
            _stepName = stepName;
            _boundaryConditionName = boundaryConditionName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PropagateBoundaryCondition(_stepName, _boundaryConditionName);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _boundaryConditionName;
        }
    }
}
