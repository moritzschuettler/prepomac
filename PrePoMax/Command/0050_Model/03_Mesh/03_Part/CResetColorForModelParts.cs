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
    class CResetColorForModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;


        // Constructor                                                                                                              
        public CResetColorForModelParts(string[] partNames)
            : base("Reset color for model parts")
        {
            _partNames = partNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ResetColorForModelParts(_partNames);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
