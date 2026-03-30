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
    class CCreateMesh : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _partName;


        // Properites                                                                                                               
        public string PartName { get { return _partName; } }


        // Constructor                                                                                                              
        public CCreateMesh(string partName)
            : base("Create mesh")
        {
            _partName = partName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.CreateMesh(_partName);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _partName;
        }
    }
}
