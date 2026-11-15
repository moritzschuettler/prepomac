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
    [PmxType(10199)]
    class CDuplicateLoads : PreprocessCommand, ILoadNamesCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _stepName;
        [PmxKey(1)] private string[] _loadNames;


        // Properties                                                                                                               
        public string StepName { get { return _stepName; } }
        public string[] LoadNames { get { return _loadNames; } set { _loadNames = value; } }


        // Constructor                                                                                                              
        public CDuplicateLoads(string stepName, string[] loadNames)
            :base("Duplicate loads")
        {
            _stepName = stepName;
            _loadNames = loadNames;
        }
       

        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateLoads(_stepName, _loadNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "].loads", _loadNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_loadNames);
        }
    }
}
