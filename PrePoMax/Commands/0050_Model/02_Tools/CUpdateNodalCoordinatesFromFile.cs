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
    class CUpdateNodalCoordinatesFromFile : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Constructor                                                                                                              
        public CUpdateNodalCoordinatesFromFile(string fileName)
            : base("Update nodal coordinates from file")
        {
            _fileName = fileName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.UpdateNodalCoordinatesFromFile(_fileName);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
