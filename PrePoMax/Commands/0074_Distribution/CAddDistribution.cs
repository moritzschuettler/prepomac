using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddDistribution : PreprocessCommand
    {
        // Variables                                                                                                                
        private Distribution _distribution;


        // Constructor                                                                                                              
        public CAddDistribution(Distribution distribution)
            :base("Add distribution")
        {
            _distribution = distribution.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddDistribution(_distribution.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _distribution.ToString();
        }
    }
}
