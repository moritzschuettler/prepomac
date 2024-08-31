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
        private string _inputFileName;
        private string _jobName;
        private bool _onlyCheckModel;


        // Properties                                                                                                               
        public string InputFileName { get { return _inputFileName; } set { _inputFileName = value; } }


        // Constructor                                                                                                              
        public CPrepareAndRunJob(string inputFileName, string jobName, bool onlyCheckModel)
            : base("Run analysis")
        {
            _inputFileName = inputFileName;
            _jobName = jobName;
            _onlyCheckModel = onlyCheckModel;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.PrepareAndRunJob(_inputFileName, _jobName, _onlyCheckModel);
            return true;
        }
        public bool ExecuteSynchronous(Controller receiver)
        {
            receiver.PrepareAndRunJob(_inputFileName, _jobName, _onlyCheckModel, false);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _jobName.ToString();
        }
    }
}
