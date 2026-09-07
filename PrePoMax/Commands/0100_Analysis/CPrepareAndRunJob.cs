// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;
using CaeJob;
using PrePoMax.Forms;

namespace PrePoMax.Commands
{
    [Serializable]
    class CPrepareAndRunJob : AnalysisCommand, ICommandAsynchronous
    {
        // Variables                                                                                                                
        private string _jobName;
        private bool _onlyCheckModel;
        [OptionalField] private int _timeoutMilliseconds;


        // Properties                                                                                                               
        public string JobName { get { return _jobName; } }


        // Constructor                                                                                                              
        public CPrepareAndRunJob(string jobName, bool onlyCheckModel, int timeoutMilliseconds = 0)
            : base("Run analysis")
        {
            _jobName = jobName;
            _onlyCheckModel = onlyCheckModel;
            _timeoutMilliseconds = timeoutMilliseconds;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.PrepareAndRunJob(_jobName, _onlyCheckModel, true, _timeoutMilliseconds);
            //return true;
        }
        public bool ExecuteSynchronous(Controller receiver)
        {
            return receiver.PrepareAndRunJob(_jobName, _onlyCheckModel, false, _timeoutMilliseconds);
            //return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonJobEmitter.Run(writer, _jobName, _onlyCheckModel);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _jobName.ToString();
        }
    }
}
