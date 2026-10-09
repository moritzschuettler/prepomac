// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Python.Journal;
using CaeMesh;
using CaeModel;
using PrePoMax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PrePoMax.Commands
{
    [Serializable]
    class CActivateDeactivateMultiple : PreprocessCommand
    {
        // Variables                                                                                                                
        private NamedClass[] _items;
        private string[] _itemNames;
        private Type[] _itemTypes;
        private bool _activate;
        private string[] _stepNames;


        // Constructor                                                                                                              
        public CActivateDeactivateMultiple(NamedClass[] items, bool activate, string[] stepNames)
            : base("Activate/deactivate multiple")
        {
            CloneItems(items);
            _activate = activate;
            _stepNames = stepNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            if (_itemNames == null) CloneItems(_items);     // compatibility v2.4.3
            receiver.ActivateDeactivateMultiple(DecloneItems(receiver), _activate, _stepNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            // The command carries the items themselves, so the collection of each is recovered from its
            // type, and the items are grouped by that collection to keep the journal short
            // A part is stored by its name and type only, so its collection is recovered from the stored type
            Dictionary<string, List<string>> namesByCollection = new Dictionary<string, List<string>>();
            for (int i = 0; i < _items.Length; i++)
            {
                string stepName = _stepNames != null && i < _stepNames.Length ? _stepNames[i] : null;
                string collection;
                string itemName;
                if (_items[i] == null)
                {
                    collection = PythonCollectionResolver.GetCollection(_itemTypes[i], stepName);
                    itemName = _itemNames[i];
                }
                else
                {
                    collection = PythonCollectionResolver.GetCollection(_items[i], stepName);
                    itemName = _items[i].Name;
                }
                if (!namesByCollection.ContainsKey(collection)) namesByCollection[collection] = new List<string>();
                namesByCollection[collection].Add(itemName);
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
            if (_itemNames == null) CloneItems(_items);     // compatibility v2.4.3
            return base.GetCommandString() + GetArrayAsString(_itemNames);
        }
        public bool ContainsMeshSetupItem()
        {
            foreach (var item in _items)
            {
                if (item is MeshSetupItem) return true;
            }
            return false;
        }
        private void CloneItems(NamedClass[] items)
        {
            _items = new NamedClass[items.Length];
            _itemNames = new string[items.Length];
            _itemTypes = new Type[items.Length];
            //
            for (int i = 0; i < items.Length; i++)
            {
                _itemNames[i] = items[i].Name;
                //
                if (items[i] is IStoreAsNameInCommands) _itemTypes[i] = items[i].GetType();
                else _items[i] = items[i].DeepClone();
            }
        }
        private NamedClass[] DecloneItems(Controller receiver)
        {
            NamedClass[] items = new NamedClass[_items.Length];
            //
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    if (_itemTypes[i] == typeof(GeometryPart) || _itemTypes[i] == typeof(CompoundGeometryPart))
                        items[i] = receiver.GetGeometryPart(_itemNames[i]);
                    else if (_itemTypes[i] == typeof(MeshPart))
                        items[i] = receiver.GetModelPart(_itemNames[i]);
                    else if (_itemTypes[i] == typeof(ResultPart))
                        items[i] = receiver.GetResultPart(_itemNames[i]);
                    else throw new NotSupportedException();
                }
                else items[i] = _items[i].DeepClone();
            }
            return items;
        }
    }
}