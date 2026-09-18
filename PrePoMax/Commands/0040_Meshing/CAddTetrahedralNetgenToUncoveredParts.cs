// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeMesh;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    /// <summary>
    /// Gives the solid parts of a model built before v2.6.3 the Netgen tetrahedral mesh setup item that keeps
    /// the mesh they were built with, for the parts the user defined no mesh setup item for. Reading such a
    /// model adds the item to it, and the reading also closes its command history with this command, so that
    /// a regenerated model is left with the same item.
    ///
    /// It stands at the end of the history although the meshing commands are replayed before it. What the
    /// replay meshes by is decided by the version the model was written by, which the session holds on to,
    /// and not by the item; the item is what makes the mesh survive a save by this version. Placing it in
    /// front of the meshing instead would not do: a model built by a script carries no meshing command at
    /// all, since the whole script is recorded as one command.
    /// </summary>
    [Serializable]
    class CAddTetrahedralNetgenToUncoveredParts : PreprocessCommand
    {
        // Variables
        private TetrahedralNetgen _meshSetupItem;


        // Constructor
        public CAddTetrahedralNetgenToUncoveredParts()
            : base("Add tetrahedral Netgen")
        {
            _meshSetupItem = null;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            _meshSetupItem = receiver.AddTetrahedralNetgenToUncoveredSolidParts();
            return true;
        }
        // The item is built from the parts the model was left with, so only a command that added one speaks
        public override void ToPython(PythonJournalWriter writer)
        {
            if (_meshSetupItem != null) PythonMeshSetupEmitter.Emit(writer, null, _meshSetupItem);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Parts without a mesh setup item";
        }
    }
}
