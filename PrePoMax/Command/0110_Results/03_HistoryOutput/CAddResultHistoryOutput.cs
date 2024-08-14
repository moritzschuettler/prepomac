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
    class CAddResultHistoryOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private ResultHistoryOutput _resultHistoryOutput;


        // Constructor                                                                                                              
        public CAddResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
            :base("Add result history output")
        {
            _resultHistoryOutput = resultHistoryOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddResultHistoryOutput(_resultHistoryOutput.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultHistoryOutput.ToString();
        }
    }
}
