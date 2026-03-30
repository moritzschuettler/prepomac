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
    class CFindEdgesByAngleForGeometryParts : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private double _edgeAngle;


        // Constructor                                                                                                              
        public CFindEdgesByAngleForGeometryParts(string[] partNames, double edgeAngle)
            : base("Find edges by angle for geometry parts")
        {
            _partNames = partNames;
            _edgeAngle = edgeAngle;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.FindEdgesByAngleForGeometryParts(_partNames, _edgeAngle);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames) + ": " + _edgeAngle + " " +
                   StringAngleDegConverter.GetUnitAbbreviation();
        }
    }
}
