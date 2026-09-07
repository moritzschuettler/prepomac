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
    class CReplaceConstraint : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldConstraintName;
        private Constraint _newConstraint;

        // Constructor                                                                                                              
        public CReplaceConstraint(string oldConstraintName, Constraint newConstraint)
            : base("Edit constraint")
        {
            _oldConstraintName = oldConstraintName;
            _newConstraint = newConstraint.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceConstraint(_oldConstraintName, _newConstraint.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonInteractionEmitter.Constraint(writer, _oldConstraintName, _newConstraint);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldConstraintName + ", " + _newConstraint.ToString();
        }
    }
}
