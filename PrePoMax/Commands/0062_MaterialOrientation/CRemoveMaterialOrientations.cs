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
    class CRemoveMaterialOrientations : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _materialOrientationNames;


        // Constructor                                                                                                              
        public CRemoveMaterialOrientations(string[] materialOrientationNames)
            : base("Remove material orientations")
        {
            _materialOrientationNames = materialOrientationNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveMaterialOrientations(_materialOrientationNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.material_orientations", _materialOrientationNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_materialOrientationNames);
        }
    }
}
