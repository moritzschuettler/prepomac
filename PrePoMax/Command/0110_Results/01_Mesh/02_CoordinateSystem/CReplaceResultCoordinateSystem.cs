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
    class CReplaceResultCoordinateSystem : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldCoordinateSystemName;
        private CoordinateSystem _newCoordinateSystem;

        // Constructor                                                                                                              
        public CReplaceResultCoordinateSystem(string oldCoordinateSystemName, CoordinateSystem newCoordinateSystem)
            : base("Edit result coordinate system")
        {
            _oldCoordinateSystemName = oldCoordinateSystemName;
            _newCoordinateSystem = newCoordinateSystem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultCoordinateSystem(_oldCoordinateSystemName, _newCoordinateSystem.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldCoordinateSystemName + ", " + _newCoordinateSystem.ToString();
        }
    }
}
