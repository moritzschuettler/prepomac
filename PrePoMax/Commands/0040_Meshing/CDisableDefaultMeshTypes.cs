// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    /// <summary>
    /// Meshes a part the user defined no mesh setup item for by Netgen tetrahedra, whatever mesh its geometry
    /// supports. This is how PrePoMax meshed before v2.6.3, and the command stands at the head of the history
    /// of every model built by such a version, so that regenerating it produces the mesh it was built with.
    ///
    /// The mesh setup item that says the same thing is added by the command at the end of the history, and it
    /// is what the model is left with. It cannot do this work as well: it can only be added once the parts are
    /// there, and a model built by a script imports and meshes within a single recorded command, so there is
    /// no place in such a history where the item would stand before the meshing.
    ///
    /// Removing this command from the command history is what asks an older model to be meshed the way this
    /// version meshes it.
    /// </summary>
    [Serializable]
    class CDisableDefaultMeshTypes : PreprocessCommand
    {
        // Constructor
        public CDisableDefaultMeshTypes()
            : base("Disable default mesh types")
        {
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.DisableDefaultMeshTypes();
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Unsupported(this, "the meshing of a model built before the default mesh types has no API form");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Netgen tetrahedra";
        }
    }
}
