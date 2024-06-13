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
    class CReplaceStepControls : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private StepControls _stepControls;

        // Constructor                                                                                                              
        public CReplaceStepControls(string stepName, StepControls stepControls)
            : base("Replace step controls")
        {
            _stepName = stepName;
            _stepControls = stepControls;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceStepControls(_stepName, _stepControls.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName;
        }
    }
}
