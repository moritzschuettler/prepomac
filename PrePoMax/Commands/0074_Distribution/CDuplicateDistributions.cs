using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CDuplicateDistributions : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _distributionNames;


        // Constructor                                                                                                              
        public CDuplicateDistributions(string[] distributionNames)
            : base("Duplicate distributions")
        {
            _distributionNames = distributionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateDistributions(_distributionNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_distributionNames);
        }
    }
}
