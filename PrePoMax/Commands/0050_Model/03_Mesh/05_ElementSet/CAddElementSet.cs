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
    class CAddElementSet : PreprocessCommand
    {
        // Variables                                                                                                                
        private FeElementSet _elementSet;


        // Constructor                                                                                                              
        public CAddElementSet(FeElementSet elementSet)
            : base("Add element set")
        {
            _elementSet = elementSet.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddElementSet(_elementSet.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _elementSet.ToString();
        }
    }
}
