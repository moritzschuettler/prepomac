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
    class CActivateDeactivate : PreprocessCommand
    {
        // Variables                                                                                                                
        private NamedClass _item;
        private bool _activate;
        private string _stepName;


        // Constructor                                                                                                              
        public CActivateDeactivate(NamedClass item, bool activate, string stepName)
            : base("Activate/deactivate")
        {
            _item = item.DeepClone();
            _activate = activate;
            _stepName = stepName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ActivateDeactivate(_item.DeepClone(), _activate, _stepName);
            return true;
        }
        public override string GetCommandString()
        {
            string data = base.GetCommandString();
            if (_stepName != null) data += _stepName + ": ";
            return data + _item.ToString();
        }
        public bool ContainsMeshSetupItem()
        {
            return _item is MeshSetupItem;
        }
    }
}
