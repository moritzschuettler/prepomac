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
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CAddSurfaceInteraction : PreprocessCommand
    {
        // Variables                                                                                                                
        private SurfaceInteraction _surfaceInteraction;


        // Constructor                                                                                                              
        public CAddSurfaceInteraction(SurfaceInteraction surfaceInteraction)
            : base("Add surface interaction")
        {
            _surfaceInteraction = surfaceInteraction.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddSurfaceInteraction(_surfaceInteraction.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonInteractionEmitter.SurfaceInteraction(writer, null, _surfaceInteraction);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _surfaceInteraction.ToString();
        }
    }
}
