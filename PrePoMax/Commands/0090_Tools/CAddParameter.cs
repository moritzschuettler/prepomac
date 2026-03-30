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
    class CAddParameter : PreprocessCommand
    {
        // Variables                                                                                                                
        private EquationParameter _parameter;


        // Constructor                                                                                                              
        public CAddParameter(EquationParameter parameter)
            : base("Add parameter")
        {
            _parameter = parameter.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddParameter(_parameter.DeepClone());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _parameter.ToString();
        }
    }
}
