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
    [PmxType(10276)]
    class CReplaceMeshRefinement : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldMeshRefinementName;
        [PmxKey(1)] private FeMeshRefinement _newMeshRefinement;


        // Constructor                                                                                                              
        public CReplaceMeshRefinement(string oldMeshRefinementName, FeMeshRefinement newMeshRefinement)
            : base("Edit mesh refinement")
        {
            _oldMeshRefinementName = oldMeshRefinementName;
            _newMeshRefinement = newMeshRefinement.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceMeshSetupItem(_oldMeshRefinementName, _newMeshRefinement.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            _newMeshRefinement = (FeMeshRefinement)receiver.EditMeshSetupItemByForm(_newMeshRefinement.DeepClone());
            return Execute(receiver);
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonMeshSetupEmitter.Emit(writer, _oldMeshRefinementName, _newMeshRefinement);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldMeshRefinementName + ", " + _newMeshRefinement.ToString();
        }
    }
}
