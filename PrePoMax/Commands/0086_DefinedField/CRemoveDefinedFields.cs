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
    class CRemoveDefinedFields : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string[] _definedFieldNames;

        // Constructor                                                                                                              
        public CRemoveDefinedFields(string stepName, string[] definedFieldNames)
            :base("Remove defined field")

        {
            _stepName = stepName;
            _definedFieldNames = definedFieldNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveDefinedFields(_stepName, _definedFieldNames);
            return true;
        }

        public override string GetCommandString()
        {

            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_definedFieldNames);
        }
    }
}
