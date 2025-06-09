using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;

namespace PrePoMax.Commands
{
    [Serializable]
    class CRemoveDistributions : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _distributionNames;

        // Constructor                                                                                                              
        public CRemoveDistributions(string[] distributionsNames)
            :base("Remove distributions")
        {
            _distributionNames = distributionsNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveDistributions(_distributionNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_distributionNames);
        }
    }
}
