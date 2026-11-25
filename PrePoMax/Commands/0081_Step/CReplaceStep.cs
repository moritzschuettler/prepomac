// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [PmxType(10179)]
    class CReplaceStep : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldStepName;
        [PmxKey(1)] private Step _newStep;

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

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonStepEmitter.Replace(writer, _oldStepName, _newStep);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldStepName + ", " + _newStep.ToString();
        }
    }
}
