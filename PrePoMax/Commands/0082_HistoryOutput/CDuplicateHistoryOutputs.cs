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
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10182)]
    class CDuplicateHistoryOutputs : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string[] _historyOutputNames;


        // Constructor                                                                                                              
        public CDuplicateHistoryOutputs(string stepName, string[] historyOutputNames)
            :base("Duplicate history outputs")
        {
            _stepName = stepName;
            _historyOutputNames = historyOutputNames;
        }
       

        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateHistoryOutputs(_stepName, _historyOutputNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, PythonStepItemEmitter.GetCollection(_stepName, "history_outputs"), _historyOutputNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_historyOutputNames);
        }
    }
}
