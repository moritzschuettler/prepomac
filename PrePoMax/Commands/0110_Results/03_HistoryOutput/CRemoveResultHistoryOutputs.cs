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

using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    class CRemoveResultHistoryOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultHistoryOutputNames;


        // Constructor                                                                                                              
        public CRemoveResultHistoryOutputs(string[] resultHistoryOutputNames)
            :base("Remove result history outputs")
        {
            _resultHistoryOutputNames = resultHistoryOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultHistoryOutputs(_resultHistoryOutputNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.results.history_outputs", "remove", PythonValueFormatter.List(_resultHistoryOutputNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {

            return base.GetCommandString() + GetArrayAsString(_resultHistoryOutputNames);
        }
    }
}
