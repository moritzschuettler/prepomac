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
    class CRemoveResultCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystemNames;


        // Constructor                                                                                                              
        public CRemoveResultCoordinateSystems(string[] coordinateSystemNames)
            : base("Remove result coordinate systems")
        {
            _coordinateSystemNames = coordinateSystemNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveResultCoordinateSystems(_coordinateSystemNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystemNames);
        }
    }
}
