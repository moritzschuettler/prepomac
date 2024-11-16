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
    class COpenResults : PostprocessCommand, ICommandAsynchronous
    {
        // Variables                                                                                                                
        private string _jobName;


        // Constructor                                                                                                              
        public COpenResults(string jobName)
            : base("Open results")
        {
            _jobName = jobName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.OpenResults(_jobName);
            return true;
        }

        public bool ExecuteSynchronous(Controller receiver)
        {
            receiver.OpenResults(_jobName, false);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _jobName.ToString();
        }
    }
}
