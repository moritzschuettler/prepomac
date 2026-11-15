// PrePoMax - Copyright (C) 2016-2026 Matej BorovinĹˇek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    [PmxType(10177)]
    class CMoveSteps : PreprocessCommand
    {
        // Variables
        [PmxKey(0)] private string[] _stepNames;
        [PmxKey(1)] private int _targetIndex;


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

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.steps", "move", PythonValueFormatter.List(_stepNames),
                                         PythonValueFormatter.Format(_targetIndex)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_stepNames) +
                   " to position " + (_targetIndex + 1).ToString();
        }
    }
}