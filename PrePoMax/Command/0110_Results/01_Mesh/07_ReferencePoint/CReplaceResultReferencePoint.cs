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
    class CReplaceResultReferencePoint : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _oldReferencePointName;
        private FeReferencePoint _newReferencePoint;

        // Constructor                                                                                                              
        public CReplaceResultReferencePoint(string oldReferencePointName, FeReferencePoint newReferencePoint)
            : base("Edit result reference point")
        {
            _oldReferencePointName = oldReferencePointName;
            _newReferencePoint = newReferencePoint.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultReferencePoint(_oldReferencePointName, _newReferencePoint.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldReferencePointName + ", " + _newReferencePoint.ToString();
        }
    }
}
