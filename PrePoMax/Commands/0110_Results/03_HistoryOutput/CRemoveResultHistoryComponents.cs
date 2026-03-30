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

namespace PrePoMax.Commands
{
    [Serializable]
    class CRemoveResultHistoryComponents : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _historyResultSetName;
        private string _historyResultFieldName;
        private string[] _historyResultComponentNames;


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + _historyResultSetName + ", " + _historyResultFieldName + ", " + 
                   GetArrayAsString(_historyResultComponentNames);
        }
    }
}
