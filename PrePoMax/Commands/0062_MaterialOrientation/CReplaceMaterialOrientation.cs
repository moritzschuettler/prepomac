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
    [PmxType(10135)]
    class CReplaceMaterialOrientation : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldMaterialOrientationName;
        [PmxKey(1)] private MaterialOrientation _newMaterialOrientation;


        // Constructor                                                                                                              
        public CReplaceMaterialOrientation(string oldMaterialOrientationName,
                                           MaterialOrientation newMaterialOrientation)
            : base("Edit material orientation")
        {
            _oldMaterialOrientationName = oldMaterialOrientationName;
            _newMaterialOrientation = newMaterialOrientation.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceMaterialOrientation(_oldMaterialOrientationName, _newMaterialOrientation.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonMaterialOrientationEmitter.Replace(writer, _oldMaterialOrientationName, _newMaterialOrientation);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldMaterialOrientationName + ", " + _newMaterialOrientation.ToString();
        }
    }
}
