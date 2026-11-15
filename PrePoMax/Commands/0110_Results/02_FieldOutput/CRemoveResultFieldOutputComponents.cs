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
    [PmxType(10257)]
    class CRemoveResultFieldOutputComponents : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _resultFieldOutputName;
        [PmxKey(1)] private string[] _componentNames;


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
