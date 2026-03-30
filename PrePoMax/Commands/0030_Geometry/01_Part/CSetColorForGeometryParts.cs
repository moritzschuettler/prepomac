// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    class CSetColorForGeometryParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private System.Drawing.Color _color;


        // Constructor                                                                                                              
        public CSetColorForGeometryParts(string[] partNames, System.Drawing.Color color)
            : base("Set color for geometry parts")
        {
            _partNames = partNames;
            _color = color;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetColorForGeometryParts(_partNames, _color);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + "Color = " + _color.ToString() + ": " + GetArrayAsString(_partNames);
        }
    }
}
