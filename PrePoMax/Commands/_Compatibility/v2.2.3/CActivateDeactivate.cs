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
    [Serializable]
    [PmxType(10287)]
    class CActivateDeactivate : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private NamedClass _item;
        [PmxKey(1)] private bool _activate;
        [PmxKey(2)] private string _stepName;


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
        public override void ToPython(PythonJournalWriter writer)
        {
            string collection = PythonCollectionResolver.GetCollection(_item, _stepName);
            writer.Statement(writer.Call(collection, _activate ? "activate" : "deactivate",
                                         PythonValueFormatter.List(new string[] { _item.Name })));
            writer.CountEmittedCommand();
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
