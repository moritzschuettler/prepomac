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


namespace PrePoMax.Commands
{
    [PmxType(10226)]
    class CDuplicateJobs : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _jobNames;


        // Constructor                                                                                                              
        public CDuplicateJobs(string[] jobNames)
            : base("Duplicate jobs")
        {
            _jobNames = jobNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateJobs(_jobNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.jobs", _jobNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_jobNames);
        }
    }
}
