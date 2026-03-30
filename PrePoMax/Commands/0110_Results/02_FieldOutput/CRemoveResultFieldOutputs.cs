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
    class CRemoveResultFieldOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultFieldOutputNames;


        // Constructor                                                                                                              
        public CRemoveResultFieldOutputs(string[] resultFieldOutputNames)
            :base("Remove result field outputs")
        {
            _resultFieldOutputNames = resultFieldOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultFieldOutputs(_resultFieldOutputNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_resultFieldOutputNames);
        }
    }
}
