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
    [PmxType(10262)]
    class CRemoveResultHistoryComponents : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _historyResultSetName;
        [PmxKey(1)] private string _historyResultFieldName;
        [PmxKey(2)] private string[] _historyResultComponentNames;


        // Constructor                                                                                                              
        public CRemoveResultHistoryComponents(string historyResultSetName, string historyResultFieldName,
                                              string[] historyResultComponentNames)
            :base("Remove result history output components")
        {
            _historyResultSetName = historyResultSetName;
            _historyResultFieldName = historyResultFieldName;
            _historyResultComponentNames = historyResultComponentNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultHistoryComponents(_historyResultSetName, _historyResultFieldName, _historyResultComponentNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.results.history_outputs", "remove_components",
                                         PythonValueFormatter.Text(_historyResultSetName),
                                         PythonValueFormatter.Text(_historyResultFieldName),
                                         PythonValueFormatter.List(_historyResultComponentNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _historyResultSetName + ", " + _historyResultFieldName + ", " + 
                   GetArrayAsString(_historyResultComponentNames);
        }
    }
}
