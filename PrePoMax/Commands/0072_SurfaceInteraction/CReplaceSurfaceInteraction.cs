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
    class CReplaceSurfaceInteraction : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldSurfaceInteractionName;
        private SurfaceInteraction _newSurfaceInteraction;

        // Constructor                                                                                                              
        public CReplaceSurfaceInteraction(string oldSurfaceInteractionName, SurfaceInteraction newSurfaceInteraction)
            : base("Edit surface interaction")
        {
            _oldSurfaceInteractionName = oldSurfaceInteractionName;
            _newSurfaceInteraction = newSurfaceInteraction.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceSurfaceInteraction(_oldSurfaceInteractionName, _newSurfaceInteraction.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldSurfaceInteractionName + ", " + _newSurfaceInteraction.ToString();
        }
    }
}
