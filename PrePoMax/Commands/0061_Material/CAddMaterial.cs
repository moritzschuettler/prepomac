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
    [PmxType(10127)]
    class CAddMaterial : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Material _material;


        // Constructor                                                                                                              
        public CAddMaterial(Material material)
            : base("Add material")
        {
            _material = material.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddMaterial(_material.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonMaterialEmitter.Add(writer, _material);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _material.ToString();
        }
    }
}
