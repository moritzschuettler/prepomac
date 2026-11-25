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
    [PmxType(10175)]
    class CAddStep : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Step _step;
        [PmxKey(1)] private bool _copyBCsAndLoads;
        [PmxKey(2)] private bool _skipsAnalysisCreation;


        // Constructor                                                                                                              
        public CAddStep(Step step, bool copyBCsAndLoads)
            : base("Add step")
        {
            _step = step.DeepClone();
            _copyBCsAndLoads = copyBCsAndLoads;
            _skipsAnalysisCreation = true;  // when old file version < 2.2.9 is opened this is false
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddStep(_step.DeepClone(), _copyBCsAndLoads, _skipsAnalysisCreation);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonStepEmitter.Add(writer, _step, _copyBCsAndLoads);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _step.ToString();
        }
    }
}
