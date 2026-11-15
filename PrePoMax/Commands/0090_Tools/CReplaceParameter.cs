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
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10216)]
    class CReplaceParameter : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldParameterName;
        [PmxKey(1)] private EquationParameter _newParameter;


        // Constructor                                                                                                              
        public CReplaceParameter(string oldParameterName, EquationParameter newParameter)
            : base("Edit Parameter")
        {
            _oldParameterName = oldParameterName;
            _newParameter = newParameter.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceParameter(_oldParameterName, _newParameter.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonParameterEmitter.Replace(writer, _oldParameterName, _newParameter);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldParameterName + ", " + _newParameter.ToString();
        }
    }
}
