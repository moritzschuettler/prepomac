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
    class CRemoveResultHistoryOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultHistoryOutputNames;


        // Constructor                                                                                                              
        public CRemoveResultHistoryOutputs(string[] resultHistoryOutputNames)
            :base("Remove result history outputs")
        {
            _resultHistoryOutputNames = resultHistoryOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultHistoryOutputs(_resultHistoryOutputNames);
            return true;
        }
        public override string GetCommandString()
        {

            return base.GetCommandString() + GetArrayAsString(_resultHistoryOutputNames);
        }
    }
}
