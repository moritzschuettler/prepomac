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
using CaeGlobals;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [PmxType(10165)]
    class CAddAmplitude : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Amplitude _amplitude;


        // Constructor                                                                                                              
        public CAddAmplitude(Amplitude amplitude)
            :base("Add amplitude")
        {
            _amplitude = amplitude.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddAmplitude(_amplitude.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonFieldEmitter.Amplitude(writer, null, _amplitude);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _amplitude.ToString();
        }
    }
}
