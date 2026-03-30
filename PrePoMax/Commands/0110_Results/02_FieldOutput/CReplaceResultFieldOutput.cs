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
using CaeGlobals;
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceResultFieldOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _oldResultFieldOutputName;
        private ResultFieldOutput _newResultFieldOutput;


        // Constructor                                                                                                              
        public CReplaceResultFieldOutput(string oldResultFieldOutputName, ResultFieldOutput newResultFieldOutput)
            : base("Edit result field output")
        {
            _oldResultFieldOutputName = oldResultFieldOutputName;
            _newResultFieldOutput = newResultFieldOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultFieldOutput(_oldResultFieldOutputName, _newResultFieldOutput.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldResultFieldOutputName + ", " + _newResultFieldOutput.ToString();
        }
    }
}
