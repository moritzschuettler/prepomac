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

using PrePoMax.Python.Journal;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10202)]
    class CRemoveLoads : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string[] _loadNames;

        // Constructor                                                                                                              
        public CRemoveLoads(string stepName, string[] loadNames)
            : base("Remove loads")
        {
            _stepName = stepName;
            _loadNames = loadNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveLoads(_stepName, _loadNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].loads", _loadNames);
        }
        public override string GetCommandString()
        {

            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_loadNames);
        }
    }
}
