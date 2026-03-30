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
    class CRemoveFieldOutputs : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string[] _fieldOutputNames;


        // Constructor                                                                                                              
        public CRemoveFieldOutputs(string stepName, string[] fieldOutputNames)
            :base("Remove field outputs")
        {
            _stepName = stepName;
            _fieldOutputNames = fieldOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveFieldOutputs(_stepName, _fieldOutputNames);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_fieldOutputNames);
        }
    }
}
