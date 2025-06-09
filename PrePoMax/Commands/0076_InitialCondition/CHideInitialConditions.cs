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
    class CHideInitialConditions : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _initialConditionNames;


        // Constructor                                                                                                              
        public CHideInitialConditions(string[] initialConditionNames)
            : base("Hide initial conditions")
        {
            _initialConditionNames = initialConditionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideInitialConditions(_initialConditionNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_initialConditionNames);
        }
    }
}
