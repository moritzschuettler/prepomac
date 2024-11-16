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
    class CDuplicateResultCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystemNames;


        // Constructor                                                                                                              
        public CDuplicateResultCoordinateSystems(string[] coordinateSystemNames)
            : base("Duplicate result coordinate systems")
        {
            _coordinateSystemNames = coordinateSystemNames.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateResultCoordinateSystems(_coordinateSystemNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystemNames);
        }
    }
}
