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
using CaeGlobals;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [PmxType(10194)]
    class CPropagateBC : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string _boundaryConditionName;


        // Constructor                                                                                                              
        public CPropagateBC(string stepName, string boundaryConditionName)
            : base("Propagate BC")
        {
            _stepName = stepName;
            _boundaryConditionName = boundaryConditionName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PropagateBoundaryCondition(_stepName, _boundaryConditionName);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].bcs",
                                         "propagate", PythonValueFormatter.Text(_boundaryConditionName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _boundaryConditionName;
        }
    }
}
