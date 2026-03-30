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
    class CAddMeshSetupItem : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private MeshSetupItem _meshSetupItem;


        // Properties                                                                                                               
        public MeshSetupItem MeshSetupItem { get { return _meshSetupItem; } set { _meshSetupItem = value; } }


        // Constructor                                                                                                              
        public CAddMeshSetupItem(MeshSetupItem meshSetupItem)
            : base("Add mesh setup item")
        {
            _meshSetupItem = meshSetupItem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddMeshSetupItem(_meshSetupItem.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            _meshSetupItem = receiver.EditMeshSetupItemByForm(_meshSetupItem.DeepClone());
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _meshSetupItem.ToString();
        }
    }
}
