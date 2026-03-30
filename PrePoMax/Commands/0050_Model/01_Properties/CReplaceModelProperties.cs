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
    class CReplaceModelProperties : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _newModelName;
        private ModelProperties _newModelProperties;


        // Constructor                                                                                                              
        public CReplaceModelProperties(string newModelName, ModelProperties newModelProperties)
            : base("Replace model properties")
        {
            _newModelName = newModelName;
            _newModelProperties = newModelProperties;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceModelProperties(_newModelName, _newModelProperties);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _newModelName + ", " + _newModelProperties.ToString();
        }
    }
}
