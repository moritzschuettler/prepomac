// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;


namespace PrePoMax.Commands
{
    [Serializable]
    class CDuplicateResultFieldOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultFieldOutputNames;


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_resultFieldOutputNames);
        }
    }
}
