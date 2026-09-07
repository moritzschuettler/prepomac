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
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    class CPropagateDefinedField : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _definedFieldName;


        // Constructor                                                                                                              
        public CPropagateDefinedField(string stepName, string definedFieldName)
            : base("Propagate defined field")
        {
            _stepName = stepName;
            _definedFieldName = definedFieldName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PropagateDefinedField(_stepName, _definedFieldName);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call(PythonStepItemEmitter.GetCollection(_stepName, "defined_fields"),
                                         "propagate", PythonValueFormatter.Text(_definedFieldName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _definedFieldName;
        }
    }
}
