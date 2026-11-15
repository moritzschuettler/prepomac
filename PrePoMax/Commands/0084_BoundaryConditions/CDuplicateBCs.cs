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
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10192)]
    class CDuplicateBCs : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string[] _boundaryConditionNames;


        // Constructor                                                                                                              
        public CDuplicateBCs(string stepName, string[] boundaryConditionNames)
            :base("Duplicate boundary conditions")
        {
            _stepName = stepName;
            _boundaryConditionNames = boundaryConditionNames;
        }
       

        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateBoundaryConditions(_stepName, _boundaryConditionNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].bcs", _boundaryConditionNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_boundaryConditionNames);
        }
    }
}
