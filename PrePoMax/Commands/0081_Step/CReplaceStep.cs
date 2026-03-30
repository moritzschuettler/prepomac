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
    class CReplaceStep : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldStepName;
        private Step _newStep;

        // Constructor                                                                                                              
        public CReplaceStep(string oldStepName, Step newStep)
            : base("Edit step")
        {
            _oldStepName = oldStepName;
            _newStep = newStep.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceStep(_oldStepName, _newStep.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldStepName + ", " + _newStep.ToString();
        }
    }
}
