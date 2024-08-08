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
    class CSetColorContoursForResultPartsCommand : PostprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private bool _colorContours;


        // Constructor                                                                                                              
        public CSetColorContoursForResultPartsCommand(string[] partNames, bool colorContours)
            : base("Set color contours for result parts")
        {
            _partNames = partNames;
            _colorContours = colorContours;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetColorContoursForResultParts(_partNames, _colorContours);
            return true;
        }

        public override string GetCommandString()
        {
            string onOff = _colorContours ? "On" : "Off";
            return base.GetCommandString() + onOff + ": " + GetArrayAsString(_partNames);
        }
    }
}
