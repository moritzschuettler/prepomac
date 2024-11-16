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
    class CShowInitialConditions : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _initialConditionNames;


        // Constructor                                                                                                              
        public CShowInitialConditions(string[] initialConditionNames)
            : base("Show initial conditions")
        {
            _initialConditionNames = initialConditionNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ShowInitialConditions(_initialConditionNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_initialConditionNames);
        }
    }
}
