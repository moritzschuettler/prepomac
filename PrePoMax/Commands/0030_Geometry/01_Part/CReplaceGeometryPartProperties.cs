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
    class CReplaceGeometryPartProperties : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldPartName;
        private PartProperties _newPartProperties;


        // Constructor                                                                                                              
        public CReplaceGeometryPartProperties(string oldPartName, PartProperties newPartProperties)
            : base("Replace geometry part properties")
        {
            _oldPartName = oldPartName;
            _newPartProperties = newPartProperties; // it is a structue DeepCopy();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceGeometryPartProperties(_oldPartName, _newPartProperties);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldPartName + ", " + _newPartProperties.ToString();
        }
    }
}
