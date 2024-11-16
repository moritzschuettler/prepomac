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
    class CShowResultReferencePoints : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _referencePointNames;


        // Constructor                                                                                                              
        public CShowResultReferencePoints(string[] referencePointNames)
            : base("Show result reference points")
        {
            _referencePointNames = referencePointNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ShowResultReferencePoints(_referencePointNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_referencePointNames);
        }
    }
}
