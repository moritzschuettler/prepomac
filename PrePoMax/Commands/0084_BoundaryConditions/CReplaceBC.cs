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
    [Serializable]
    [PmxType(10196)]
    class CReplaceBC : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string _oldBoundaryConditionName;
        [PmxKey(2)] private BoundaryCondition _newBoundaryCondition;


        // Constructor                                                                                                              
        public CReplaceBC(string stepName, string oldBoundaryConditionName, BoundaryCondition newBoundaryCondition)
            : base("Edit BC")
        {
            _stepName = stepName;
            _oldBoundaryConditionName = oldBoundaryConditionName;
            _newBoundaryCondition = newBoundaryCondition.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceBoundaryCondition(_stepName, _oldBoundaryConditionName, _newBoundaryCondition.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonBoundaryConditionEmitter.Replace(writer, _stepName, _oldBoundaryConditionName,
                                                   _newBoundaryCondition);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _oldBoundaryConditionName + ", " + _newBoundaryCondition.ToString();
        }
    }
}
