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
    class CSetCurrentResults : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _resultsName;


        // Constructor                                                                                                              
        public CSetCurrentResults(string resultsName)
            : base("Set current results")
        {
            _resultsName = resultsName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetCurrentResults(_resultsName);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultsName;
        }
    }
}
