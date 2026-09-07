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
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CActivateDeactivateMultilpe : PreprocessCommand
    {
        // Variables                                                                                                                
        private NamedClass[] _items;
        private bool _activate;
        private string[] _stepNames;


        // Constructor                                                                                                              
        public CActivateDeactivateMultilpe(NamedClass[] items, bool activate, string[] stepNames)
            : base("Activate/deactivate multiple")
        {
            _items = items.DeepClone();
            _activate = activate;
            _stepNames = stepNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ActivateDeactivateMultiple(_items.DeepClone(), _activate, _stepNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            Dictionary<string, List<string>> namesByCollection = new Dictionary<string, List<string>>();
            for (int i = 0; i < _items.Length; i++)
            {
                string stepName = _stepNames != null && i < _stepNames.Length ? _stepNames[i] : null;
                string collection = PythonCollectionResolver.GetCollection(_items[i], stepName);
                if (!namesByCollection.ContainsKey(collection)) namesByCollection[collection] = new List<string>();
                namesByCollection[collection].Add(_items[i].Name);
            }
            foreach (var entry in namesByCollection)
            {
                writer.Statement(writer.Call(entry.Key, _activate ? "activate" : "deactivate",
                                             PythonValueFormatter.List(entry.Value)));
            }
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            string[] names = new string[_items.Length];
            for (int i = 0; i < names.Length; i++) names[i] = _items[i].Name;
            return base.GetCommandString() + GetArrayAsString(names);
        }
    }
}
