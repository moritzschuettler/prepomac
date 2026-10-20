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
    class CRemoveCompositeLayups : PreprocessCommand
    {
        // Variables
        private string[] _compositeLayupNames;


        // Constructor
        public CRemoveCompositeLayups(string[] compositeLayupNames)
            : base("Remove composite layups")
        {
            _compositeLayupNames = compositeLayupNames;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveCompositeLayups(_compositeLayupNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_compositeLayupNames);
        }
    }
}
