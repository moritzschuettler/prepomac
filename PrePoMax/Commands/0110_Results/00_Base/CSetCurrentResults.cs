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
using System.IO;

namespace PrePoMax.Commands
{
    [Serializable]
    class CSetCurrentResults : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _resultsName;


        // Properties                                                                                                               
        public string ResultsName { get { return _resultsName; } }
        public string JobName { get { return Path.GetFileNameWithoutExtension(_resultsName); } }


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
