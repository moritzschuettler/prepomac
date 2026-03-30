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
    class CFlipFaceOrientations : PreprocessCommand
    {
        // Variables                                                                                                                
        private GeometrySelection _geometrySelection;


        // Constructor                                                                                                              
        public CFlipFaceOrientations(GeometrySelection geometrySelection)
            : base("Flip face orientations")
        {
            _geometrySelection = geometrySelection.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.FlipFaceOrientations(_geometrySelection.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _geometrySelection.ToString();
        }
    }
}
