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
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10265)]
    class CReplaceResultHistoryOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldResultHistoryOutputName;
        [PmxKey(1)] private ResultHistoryOutput _newResultHistoryOutput;


        // Properties                                                                                                               
        public ResultHistoryOutput NewResultHistoryOutput
        {
            get { return _newResultHistoryOutput; }
            set { _newResultHistoryOutput = value; }
        }


        // Constructor                                                                                                              
        public CReplaceResultHistoryOutput(string oldHistoryOutputName, ResultHistoryOutput newResultHistoryOutput)
            : base("Edit result history output")
        {
            _oldResultHistoryOutputName = oldHistoryOutputName;
            _newResultHistoryOutput = newResultHistoryOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultHistoryOutput(_oldResultHistoryOutputName, _newResultHistoryOutput.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonResultOutputEmitter.HistoryOutput(writer, _oldResultHistoryOutputName, _newResultHistoryOutput);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldResultHistoryOutputName + ", " + _newResultHistoryOutput.ToString();
        }
    }
}
