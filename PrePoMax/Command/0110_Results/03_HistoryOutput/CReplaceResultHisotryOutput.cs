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
    class CReplaceResultHistoryOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _oldResultHistoryOutputName;
        private ResultHistoryOutput _newResultHistoryOutput;


        // Constructor                                                                                                              
        public CReplaceResultHistoryOutput(string oldHistoryOutputName, ResultHistoryOutput newResultHistoryOutput)
            : base("Edit result history output")
        {
            _oldResultHistoryOutputName = oldHistoryOutputName;
            _newResultHistoryOutput = newResultHistoryOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultHistoryOutput(_oldResultHistoryOutputName, _newResultHistoryOutput.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldResultHistoryOutputName + ", " + _newResultHistoryOutput.ToString();
        }
    }
}
