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
    class CReplaceModelCoordinateSystem : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldCoordinateSystemName;
        private CoordinateSystem _newCoordinateSystem;

        // Constructor                                                                                                              
        public CReplaceModelCoordinateSystem(string oldCoordinateSystemName, CoordinateSystem newCoordinateSystem)
            : base("Edit model coordinate system")
        {
            _oldCoordinateSystemName = oldCoordinateSystemName;
            _newCoordinateSystem = newCoordinateSystem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceModelCoordinateSystem(_oldCoordinateSystemName, _newCoordinateSystem.DeepClone(), true);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldCoordinateSystemName + ", " + _newCoordinateSystem.ToString();
        }
    }
}
