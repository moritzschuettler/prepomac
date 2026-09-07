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
    class CRemoveResultFieldOutputComponents : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _resultFieldOutputName;
        private string[] _componentNames;


        // Constructor                                                                                                              
        public CRemoveResultFieldOutputComponents(string resultFieldOutputName, string[] componentNames)
            :base("Remove result field output components")
        {
            _resultFieldOutputName = resultFieldOutputName;
            _componentNames = componentNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultFieldOutputComponents(_resultFieldOutputName, _componentNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.results.field_outputs", "remove_components",
                                         PythonValueFormatter.Text(_resultFieldOutputName),
                                         PythonValueFormatter.List(_componentNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultFieldOutputName + ": " + GetArrayAsString(_componentNames);
        }
    }
}
