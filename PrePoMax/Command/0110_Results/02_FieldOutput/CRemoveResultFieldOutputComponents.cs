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
    class CRemoveResultFieldOutputComponents : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _resultFieldOutputName;
        private string[] _componentNames;


        // Constructor                                                                                                              
        public CRemoveResultFieldOutputComponents(string resultFieldOutputName, string[] componentNames)
            :base("Remove result field output components")
        {
            _resultFieldOutputName = resultFieldOutputName;
            _componentNames = componentNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultFieldOutputComponents(_resultFieldOutputName, _componentNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultFieldOutputName + ": " + GetArrayAsString(_componentNames);
        }
    }
}
