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
    class CAddResultCoordinateSystem : PreprocessCommand
    {
        // Variables                                                                                                                
        private CoordinateSystem _coordinateSystem;


        // Constructor                                                                                                              
        public CAddResultCoordinateSystem(CoordinateSystem coordinateSystem)
            : base("Add result coordinate system")
        {
            _coordinateSystem = coordinateSystem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddResultCoordinateSystem(_coordinateSystem.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _coordinateSystem.ToString();
        }
    }
}
