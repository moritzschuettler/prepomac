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
    class CHideModelCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystems;


        // Constructor                                                                                                              
        public CHideModelCoordinateSystems(string[] coordinateSystems)
            : base("Hide model coordinate systems")
        {
            _coordinateSystems = coordinateSystems.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideModelCoordinateSystems(_coordinateSystems);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystems);
        }
    }
}
