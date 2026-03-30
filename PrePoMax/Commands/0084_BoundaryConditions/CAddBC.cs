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
    class CAddBC : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private BoundaryCondition _boundaryCondition;


        // Constructor                                                                                                              
        public CAddBC(string stepName, BoundaryCondition boundaryCondition)
            : base("Add BC")
        {
            _stepName = stepName;
            _boundaryCondition = boundaryCondition.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddBoundaryCondition(_stepName, _boundaryCondition.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _boundaryCondition.ToString();
        }
    }
}
