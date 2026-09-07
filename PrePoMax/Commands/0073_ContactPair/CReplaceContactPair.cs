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
    class CReplaceContactPair : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldContactPairName;
        private ContactPair _newContactPair;


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
