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
using CaeGlobals;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [PmxType(10185)]
    class CReplaceHistoryOutput : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string _oldHistoryOutputName;
        [PmxKey(2)] private HistoryOutput _newHistoryOutput;


        // Constructor                                                                                                              
        public CReplaceHistoryOutput(string stepName, string oldHistoryOutputName, HistoryOutput newHistoryOutput)
            : base("Edit history output")
        {
            _stepName = stepName;
            _oldHistoryOutputName = oldHistoryOutputName;
            _newHistoryOutput = newHistoryOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceHistoryOutput(_stepName, _oldHistoryOutputName, _newHistoryOutput.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonOutputEmitter.HistoryOutput(writer, _stepName, _oldHistoryOutputName, _newHistoryOutput);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _oldHistoryOutputName + ", " + _newHistoryOutput.ToString();
        }
    }
}
