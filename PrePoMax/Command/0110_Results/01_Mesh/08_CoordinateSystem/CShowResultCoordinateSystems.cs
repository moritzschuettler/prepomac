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
    class CShowResultCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystemNames;


        // Constructor                                                                                                              
        public CShowResultCoordinateSystems(string[] coordinateSystemNames)
            : base("Show result coordinate systems")
        {
            _coordinateSystemNames = coordinateSystemNames.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ShowResultCoordinateSystems(_coordinateSystemNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystemNames);
        }
    }
}
