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
    class CReplaceSurface : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldSurfaceName;
        private FeSurface _newSurface;

        // Constructor                                                                                                              
        public CReplaceSurface(string oldSurfaceName, FeSurface newSurface)
            : base("Edit surface")
        {
            _oldSurfaceName = oldSurfaceName;
            _newSurface = newSurface.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceSurface(_oldSurfaceName, _newSurface.DeepClone(), true);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldSurfaceName + ", " + _newSurface.ToString();
        }
    }
}
