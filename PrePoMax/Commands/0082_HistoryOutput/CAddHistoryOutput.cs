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

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddHistoryOutput : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private HistoryOutput _historyOutput;


        // Constructor                                                                                                              
        public CAddHistoryOutput(string stepName, HistoryOutput historyOutput)
            :base("Add history output")
        {
            _stepName = stepName;
            _historyOutput = historyOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddHistoryOutput(_stepName, _historyOutput.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _historyOutput.ToString();
        }
    }
}
