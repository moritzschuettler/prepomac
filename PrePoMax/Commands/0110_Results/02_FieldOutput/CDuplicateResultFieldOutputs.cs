// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;


using PrePoMax.Python.Journal;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(10256)]
    class CDuplicateResultFieldOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _resultFieldOutputNames;


        // Constructor                                                                                                              
        public CDuplicateResultFieldOutputs(string[] resultFieldOutputNames)
            : base("Duplicate result field outputs")
        {
            _resultFieldOutputNames = resultFieldOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateResultFieldOutputs(_resultFieldOutputNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.results.field_outputs", "duplicate", PythonValueFormatter.List(_resultFieldOutputNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_resultFieldOutputNames);
        }
    }
}
