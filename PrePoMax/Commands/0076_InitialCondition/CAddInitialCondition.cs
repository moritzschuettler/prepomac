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
    [PmxType(10169)]
    class CAddInitialCondition : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private InitialCondition _initialCondition;


        // Constructor                                                                                                              
        public CAddInitialCondition(InitialCondition initialCondition)
            : base("Add initial condition")
        {
            _initialCondition = initialCondition.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddInitialCondition(_initialCondition.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonFieldEmitter.InitialCondition(writer, null, _initialCondition);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _initialCondition.ToString();
        }
    }
}
