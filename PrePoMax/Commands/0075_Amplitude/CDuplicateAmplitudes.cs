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
    class CDuplicateAmplitudes : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _amplitudeNames;


        // Constructor                                                                                                              
        public CDuplicateAmplitudes(string[] amplitudeNames)
            : base("Duplicate amplitudes")
        {
            _amplitudeNames = amplitudeNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.DuplicateAmplitudes(_amplitudeNames);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_amplitudeNames);
        }
    }
}
