// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(10151)]
    class CReplaceSurfaceInteraction : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldSurfaceInteractionName;
        [PmxKey(1)] private SurfaceInteraction _newSurfaceInteraction;

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
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonInteractionEmitter.SurfaceInteraction(writer, _oldSurfaceInteractionName, _newSurfaceInteraction);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldSurfaceInteractionName + ", " + _newSurfaceInteraction.ToString();
        }
    }
}
