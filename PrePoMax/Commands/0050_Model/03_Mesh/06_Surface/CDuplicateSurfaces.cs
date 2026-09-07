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
    class CDuplicateSurfaces : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _surfaceNames;


        // Constructor                                                                                                              
        public CDuplicateSurfaces(string[] surfaceNames)
            : base("Duplicate surfaces")
        {
            _surfaceNames = surfaceNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateSurfaces(_surfaceNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.model.surfaces", _surfaceNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_surfaceNames);
        }
    }
}
