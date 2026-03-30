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
    class CAddContactPair : PreprocessCommand
    {
        // Variables                                                                                                                
        private ContactPair _contactPair;
        private bool _update;


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + _contactPair.ToString();
        }
    }
}
