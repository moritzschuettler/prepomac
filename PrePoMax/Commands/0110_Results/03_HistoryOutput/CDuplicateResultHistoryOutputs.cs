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
    class CDuplicateResultHistoryOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultHistoryOutputNames;


        // Constructor                                                                                                              
        public CDuplicateResultHistoryOutputs(string[] resultHistoryOutputNames)
            : base("Duplicate result history outputs")
        {
            _resultHistoryOutputNames = resultHistoryOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateResultHistoryOutputs(_resultHistoryOutputNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_resultHistoryOutputNames);
        }
    }
}
