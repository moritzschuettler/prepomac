using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeGlobals;
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddResultFieldOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private ResultFieldOutput _resultFieldOutput;


        // Constructor                                                                                                              
        public CAddResultFieldOutput(ResultFieldOutput resultFieldOutput)
            :base("Add result field output")
        {
            _resultFieldOutput = resultFieldOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddResultFieldOutput(_resultFieldOutput.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultFieldOutput.ToString();
        }
    }
}
