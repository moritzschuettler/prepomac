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
    class CReplaceStepControls : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private StepControls _stepControls;

        // Constructor                                                                                                              
        public CReplaceStepControls(string stepName, StepControls stepControls)
            : base("Replace step controls")
        {
            _stepName = stepName;
            _stepControls = stepControls;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceStepControls(_stepName, _stepControls.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName;
        }
    }
}
