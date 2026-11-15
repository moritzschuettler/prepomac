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
    [PmxType(10158)]
    class CReplaceContactPair : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldContactPairName;
        [PmxKey(1)] private ContactPair _newContactPair;


        // Constructor                                                                                                              
        public CReplaceContactPair(string oldContactPairName, ContactPair newContactPair)
            : base("Edit contact pair")
        {
            _oldContactPairName = oldContactPairName;
            _newContactPair = newContactPair.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceContactPair(_oldContactPairName, _newContactPair.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonInteractionEmitter.ContactPair(writer, _oldContactPairName, _newContactPair);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldContactPairName + ", " + _newContactPair.ToString();
        }
    }
}
