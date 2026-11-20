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
    [PmxType(10152)]
    class CAddContactPair : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private ContactPair _contactPair;
        [PmxKey(1)] private bool _update;


        // Constructor                                                                                                              
        public CAddContactPair(ContactPair contactPair, bool update)
            : base("Add contact pair")
        {
            _contactPair = contactPair.DeepClone();
            _update = update;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddContactPair(_contactPair.DeepClone(), _update);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonInteractionEmitter.ContactPair(writer, null, _contactPair);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _contactPair.ToString();
        }
    }
}
