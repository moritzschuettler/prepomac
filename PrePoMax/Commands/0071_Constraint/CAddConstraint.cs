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


namespace PrePoMax.Commands
{
    [Serializable]
    class CAddConstraint : PreprocessCommand
    {
        // Variables                                                                                                                
        private Constraint _constraint;
        private bool _update;


        // Constructor                                                                                                              
        public CAddConstraint(Constraint constraint, bool update)
            : base("Add constraint")
        {
            _constraint = constraint.DeepClone();
            _update = update;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddConstraint(_constraint.DeepClone(), _update);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _constraint.ToString();
        }
    }
}
