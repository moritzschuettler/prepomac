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
    class CDuplicateModelCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystemNames;


        // Constructor                                                                                                              
        public CDuplicateModelCoordinateSystems(string[] coordinateSystemNames)
            : base("Duplicate model coordinate systems")
        {
            _coordinateSystemNames = coordinateSystemNames.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateModelCoordinateSystems(_coordinateSystemNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystemNames);
        }
    }
}
