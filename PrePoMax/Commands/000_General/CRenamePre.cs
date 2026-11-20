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
    [PmxType(10032)]
    class CRenamePre : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Type _itemType;
        [PmxKey(1)] private string _itemName;
        [PmxKey(2)] private string _newName;
        [PmxKey(3)] private string _stepName;
        [PmxKey(4)] private ViewGeometryModelResults _view;


        // Variables                                                                                                                
        public Type ItemType { get { return _itemType; } }
        public ViewGeometryModelResults View { get { return _view; } }


        // Constructor                                                                                                              
        public CRenamePre(Type itemType, string itemName, string newName, string stepName, ViewGeometryModelResults view)
            : base("Rename")
        {
            _itemType = itemType;
            _itemName = itemName;
            _newName = newName;
            _stepName = stepName;
            _view = view;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.Rename(_itemType, _itemName, _newName, _stepName, _view);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            string collection = PythonCollectionResolver.GetCollection(_itemType, _stepName, _view);
            writer.Statement(writer.Call(collection, "rename", PythonValueFormatter.Text(_itemName),
                                         PythonValueFormatter.Text(_newName)));
            writer.RenameName(collection, _itemName, _newName);
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _itemName + " to " + _newName;
        }
    }
}
