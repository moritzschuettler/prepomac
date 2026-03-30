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
    class CAddResultReferencePoint : PostprocessCommand
    {
        // Variables                                                                                                                
        private FeReferencePoint _referencePoint;


        // Constructor                                                                                                              
        public CAddResultReferencePoint(FeReferencePoint referencePoint)
            : base("Add result reference point")
        {
            _referencePoint = referencePoint.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddResultReferencePoint(_referencePoint.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _referencePoint.ToString();
        }
    }
}
