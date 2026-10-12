// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace PrePoMax.Python.Journal
{
    // The verbs every named model collection shares. They are gathered here because the same four calls
    // are emitted by every area, and only the collection path and the item differ.
    public static class PythonModelItemEmitter
    {
        // Methods                                                                                                                  
        public static void AddFromSelection(PythonJournalWriter writer, string collection, NamedClass item,
                                            Selection creationData)
        {
            // A selection based item is rebuilt from the selection it was created with, never from the
            // internal set the controller converted it into
            string selectionVariable = PythonSelectionEmitter.Emit(writer, creationData);
            //
            writer.Statement(writer.Call(collection, "add", PythonValueFormatter.Text(item.Name),
                                         selectionVariable));
            EmitState(writer, collection, item);
            writer.CountEmittedCommand();
        }
        public static void ReplaceFromSelection(PythonJournalWriter writer, string collection, string oldName,
                                                NamedClass item, Selection creationData)
        {
            string selectionVariable = PythonSelectionEmitter.Emit(writer, creationData);
            //
            writer.Statement(writer.Call(collection, "replace", PythonValueFormatter.Text(oldName),
                                         PythonValueFormatter.Text(item.Name), selectionVariable));
            EmitState(writer, collection, item);
            writer.CountEmittedCommand();
        }
        public static void Remove(PythonJournalWriter writer, string collection, string[] names)
        {
            writer.Statement(writer.Call(collection, "remove", PythonValueFormatter.List(names)));
            writer.RemoveNames(collection, names);
            writer.CountEmittedCommand();
        }
        public static void Duplicate(PythonJournalWriter writer, string collection, string[] names)
        {
            writer.Statement(writer.Call(collection, "duplicate", PythonValueFormatter.List(names)));
            writer.CountEmittedCommand();
        }
        // An item is added active, so only the deactivated state needs a statement of its own
        public static void EmitState(PythonJournalWriter writer, string collection, NamedClass item)
        {
            if (item.Active) return;
            //
            writer.Statement(writer.Call(collection, "deactivate",
                                         PythonValueFormatter.List(new string[] { item.Name })));
        }
    }
}
