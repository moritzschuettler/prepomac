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
using CaeJob;

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceJob : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldJobName;
        private AnalysisJob _newJob;

        // Constructor                                                                                                              
        public CReplaceJob(string oldJobName, AnalysisJob newJob)
            : base("Edit analysis")
        {
            _oldJobName = oldJobName;
            _newJob = newJob.DeepClone();
            _newJob.ClearFileContents();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceJob(_oldJobName, _newJob.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldJobName + ", " + _newJob.ToString();
        }
    }
}
