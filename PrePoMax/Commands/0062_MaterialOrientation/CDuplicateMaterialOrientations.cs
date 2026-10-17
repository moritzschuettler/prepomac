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
    class CDuplicateMaterialOrientations : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _materialOrientationNames;


        // Constructor                                                                                                              
        public CDuplicateMaterialOrientations(string[] materialOrientationNames)
            : base("Duplicate material orientations")
        {
            _materialOrientationNames = materialOrientationNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateMaterialOrientations(_materialOrientationNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.model.material_orientations", _materialOrientationNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_materialOrientationNames);
        }
    }
}
