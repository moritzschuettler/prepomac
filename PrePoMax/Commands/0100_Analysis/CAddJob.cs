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
using PrePoMax.Forms;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddJob : PreprocessCommand
    {
        // Variables                                                                                                                
        private AnalysisJob _job;


        // Constructor                                                                                                              
        public CAddJob(AnalysisJob job)
            : base("Add analysis")
        {
            _job = job.DeepClone();
            _job.ClearFileContents();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddJob(_job.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _job.ToString();
        }
    }
}
