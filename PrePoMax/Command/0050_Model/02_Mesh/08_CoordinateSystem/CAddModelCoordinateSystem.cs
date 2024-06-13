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
    class CAddModelCoordinateSystem : PreprocessCommand
    {
        // Variables                                                                                                                
        private CoordinateSystem _coordinateSystem;


        // Constructor                                                                                                              
        public CAddModelCoordinateSystem(CoordinateSystem coordinateSystem)
            : base("Add model coordinate system")
        {
            _coordinateSystem = coordinateSystem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddModelCoordinateSystem(_coordinateSystem.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _coordinateSystem.ToString();
        }
    }
}
