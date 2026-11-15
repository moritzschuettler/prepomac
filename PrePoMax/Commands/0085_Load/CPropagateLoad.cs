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
    [PmxType(10201)]
    class CPropagateLoad : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string _loadName;


        // Constructor                                                                                                              
        public CPropagateLoad(string stepName, string loadName)
            : base("Propagate load")
        {
            _stepName = stepName;
            _loadName = loadName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PropagateLoad(_stepName, _loadName);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].loads",
                                         "propagate", PythonValueFormatter.Text(_loadName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _loadName;
        }
    }
}
