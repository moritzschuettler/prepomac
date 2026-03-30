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
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddDefinedField : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private DefinedField _definedField;


        // Constructor                                                                                                              
        public CAddDefinedField(string stepName, DefinedField definedField)
            :base("Add defined field")
        {
            _stepName = stepName;
            _definedField = definedField.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddDefinedField(_stepName, _definedField.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _definedField.ToString();
        }
    }
}
