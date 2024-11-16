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
    class CResetColorForGeometryParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;


        // Constructor                                                                                                              
        public CResetColorForGeometryParts(string[] partNames)
            : base("Reset color for geometry parts")
        {
            _partNames = partNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ResetColorForGeometryParts(_partNames);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
