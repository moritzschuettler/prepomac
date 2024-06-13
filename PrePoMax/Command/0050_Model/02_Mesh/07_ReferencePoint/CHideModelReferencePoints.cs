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
    class CHideModelReferencePoints : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _referencePoints;


        // Constructor                                                                                                              
        public CHideModelReferencePoints(string[] referencePoints)
            : base("Hide model reference points")
        {
            _referencePoints = referencePoints;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideModelReferencePoints(_referencePoints);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_referencePoints);
        }
    }
}
