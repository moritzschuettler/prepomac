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
    class CRemoveResultFieldOutputs : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _resultFieldOutputNames;


        // Constructor                                                                                                              
        public CRemoveResultFieldOutputs(string[] resultFieldOutputNames)
            :base("Remove result field outputs")
        {
            _resultFieldOutputNames = resultFieldOutputNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultFieldOutputs(_resultFieldOutputNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_resultFieldOutputNames);
        }
    }
}
