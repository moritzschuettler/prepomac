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
    class CHideResultCoordinateSystems : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _coordinateSystems;


        // Constructor                                                                                                              
        public CHideResultCoordinateSystems(string[] coordinateSystems)
            : base("Hide result coordinate systems")
        {
            _coordinateSystems = coordinateSystems.ToArray();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideResultCoordinateSystems(_coordinateSystems);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_coordinateSystems);
        }
    }
}
