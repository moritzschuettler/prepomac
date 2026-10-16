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
    class CAddMaterialOrientation : PreprocessCommand
    {
        // Variables                                                                                                                
        private MaterialOrientation _materialOrientation;


        // Constructor                                                                                                              
        public CAddMaterialOrientation(MaterialOrientation materialOrientation)
            : base("Add material orientation")
        {
            _materialOrientation = materialOrientation.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddMaterialOrientation(_materialOrientation.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonMaterialOrientationEmitter.Add(writer, _materialOrientation);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _materialOrientation.ToString();
        }
    }
}
