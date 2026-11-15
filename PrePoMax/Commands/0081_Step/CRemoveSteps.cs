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
    [PmxType(10178)]
    class CRemoveSteps : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _stepNames;


        // Constructor                                                                                                              
        public CRemoveSteps(string[] stepNames)
            : base("Remove steps")
        {
            _stepNames = stepNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveSteps(_stepNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.steps", _stepNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_stepNames);
        }
    }
}
