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
    [PmxType(10270)]
    class CAddMeshRefinement : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        [PmxKey(0)] private FeMeshRefinement _meshRefinement;


        // Constructor                                                                                                              
        public CAddMeshRefinement(FeMeshRefinement meshRefinement)
            : base("Add mesh refinement")
        {
            _meshRefinement = meshRefinement.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddMeshSetupItem(_meshRefinement.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            _meshRefinement = (FeMeshRefinement)receiver.EditMeshSetupItemByForm(_meshRefinement.DeepClone());
            return Execute(receiver);
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonMeshSetupEmitter.Emit(writer, null, _meshRefinement);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _meshRefinement.ToString();
        }
    }
}
