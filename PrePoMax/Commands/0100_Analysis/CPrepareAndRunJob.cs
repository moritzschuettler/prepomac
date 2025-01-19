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
    class CPrepareAndRunJob : AnalysisCommand, ICommandAsynchronous
    {
        // Variables                                                                                                                
        private string _jobName;
        private bool _onlyCheckModel;


        // Properties                                                                                                               
        public string JobName { get { return _jobName; } }


        // Constructor                                                                                                              
        public CPrepareAndRunJob(string jobName, bool onlyCheckModel)
            : base("Run analysis")
        {
            _jobName = jobName;
            _onlyCheckModel = onlyCheckModel;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.PrepareAndRunJob(_jobName, _onlyCheckModel);
            //return true;
        }
        public bool ExecuteSynchronous(Controller receiver)
        {
            return receiver.PrepareAndRunJob(_jobName, _onlyCheckModel, false);
            //return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _jobName.ToString();
        }
    }
}
