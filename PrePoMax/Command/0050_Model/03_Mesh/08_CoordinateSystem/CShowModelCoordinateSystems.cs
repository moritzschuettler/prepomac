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
    class CShowModelCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystemNames;


        // Constructor                                                                                                              
        public CShowModelCoordinateSystems(string[] coordinateSystemNames)
            : base("Show model coordinate systems")
        {
            _coordinateSystemNames = coordinateSystemNames.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ShowModelCoordinateSystems(_coordinateSystemNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystemNames);
        }
    }
}
