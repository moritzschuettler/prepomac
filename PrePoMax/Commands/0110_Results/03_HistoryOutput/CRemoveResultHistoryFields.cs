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
    class CRemoveResultHistoryFields : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _historyResultSetName;
        private string[] _historyResultFieldNames;
        

        // Constructor                                                                                                              
        public CRemoveResultHistoryFields(string historyResultSetName, string[] historyResultFieldNames)
            :base("Remove result history output fields")
        {
            _historyResultSetName = historyResultSetName;
            _historyResultFieldNames = historyResultFieldNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultHistoryFields(_historyResultSetName, _historyResultFieldNames);
            return true;
        }
        public override string GetCommandString()
        {

            return base.GetCommandString() + _historyResultSetName + ", " + GetArrayAsString(_historyResultFieldNames);
        }
    }
}
