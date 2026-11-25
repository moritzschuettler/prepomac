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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [PmxType(10173)]
    class CReplaceInitialCondition : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldInitialConditionName;
        [PmxKey(1)] private InitialCondition _newInitialCondition;


        // Constructor                                                                                                              
        public CReplaceInitialCondition(string oldInitialConditionName, InitialCondition newInitialCondition)
            : base("Edit initial condition")
        {
            _oldInitialConditionName = oldInitialConditionName;
            _newInitialCondition = newInitialCondition.DeepClone();
        }


        // Methods                                                                                                                  
        [OnDeserialized]
        private void ConvertInitialVelocity(StreamingContext context)
        {
            if (_newInitialCondition is InitialVelocity initialVelocity) _newInitialCondition = initialVelocity.ToInitialTranslationalVelocity();
        }
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceInitialCondition(_oldInitialConditionName, _newInitialCondition.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonFieldEmitter.InitialCondition(writer, _oldInitialConditionName, _newInitialCondition);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldInitialConditionName + ", " + _newInitialCondition.ToString();
        }
    }
}
