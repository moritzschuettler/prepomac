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
    class CPropagateHistoryOutput : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _historyOutputName;


        // Constructor                                                                                                              
        public CPropagateHistoryOutput(string stepName, string historyOutputName)
            : base("Propagate history output")
        {
            _stepName = stepName;
            _historyOutputName = historyOutputName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PropagateHistoryOutput(_stepName, _historyOutputName);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _historyOutputName;
        }
    }
}
