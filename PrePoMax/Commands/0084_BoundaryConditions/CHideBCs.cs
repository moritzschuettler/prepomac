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
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CHideBCs : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string[] _boundaryConditionNames;


        // Constructor                                                                                                              
        public CHideBCs(string stepName, string[] boundaryConditionNames)
            : base("Hide boundary conditions")
        {
            _stepName = stepName;
            _boundaryConditionNames = boundaryConditionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideBoundaryConditions(_stepName, _boundaryConditionNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].bcs", "hide", PythonValueFormatter.List(_boundaryConditionNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_boundaryConditionNames);
        }
    }
}
