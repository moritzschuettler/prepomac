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
    class CReplaceElementSet : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldElementSetName;
        private FeElementSet _newElementSet;

        // Constructor                                                                                                              
        public CReplaceElementSet(string oldElementSetName, FeElementSet newElementSet)
            : base("Edit element set")
        {
            _oldElementSetName = oldElementSetName;
            _newElementSet = newElementSet.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceElementSet(_oldElementSetName, _newElementSet.DeepClone(), true);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldElementSetName + ", " + _newElementSet.ToString();
        }
    }
}
