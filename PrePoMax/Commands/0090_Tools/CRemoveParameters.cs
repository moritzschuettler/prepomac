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
    class CRemoveParameters : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _parameterNames;


        // Constructor                                                                                                              
        public CRemoveParameters(string[] parameterNames)
            : base("Remove parameters")
        {
            _parameterNames = parameterNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveParameters(_parameterNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.parameters", _parameterNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_parameterNames);
        }
    }
}
