// PrePoMax - Copyright (C) 2016-2026 Matej BorovinĹˇek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
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
    class CMoveSteps : PreprocessCommand
    {
        // Variables
        private string[] _stepNames;
        private int _targetIndex;


        // Constructor
        public CMoveSteps(string[] stepNames, int targetIndex)
            : base("Move steps")
        {
            if (stepNames == null) throw new ArgumentNullException("stepNames");
            _stepNames = (string[])stepNames.Clone();
            _targetIndex = targetIndex;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.MoveSteps(_stepNames, _targetIndex);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_stepNames) +
                   " to position " + (_targetIndex + 1).ToString();
        }
    }
}