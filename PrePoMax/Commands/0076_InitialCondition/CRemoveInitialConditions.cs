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
    [Serializable]
    [PmxType(10172)]
    class CRemoveInitialConditions : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _initialConditionNames;


        // Constructor                                                                                                              
        public CRemoveInitialConditions(string[] initialConditionNames)
            : base("Remove initial conditions")
        {
            _initialConditionNames = initialConditionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveInitialConditions(_initialConditionNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.initial_conditions", _initialConditionNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_initialConditionNames);
        }
    }
}
