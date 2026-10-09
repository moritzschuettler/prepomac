// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeMesh;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    /// <summary>
    /// Gives the parts of a model built before v2.6.3 the Netgen mesh setup items that keep the mesh they were
    /// built with, for the parts the user defined no mesh setup item for. One item is added for each set of
    /// Netgen settings the meshing parameters gave those parts, since these settings now belong to the item.
    /// Reading such a model adds the items to it, and the reading also closes its command history with this
    /// command, so that a regenerated model is left with the same items.
    ///
    /// It stands at the end of the history although the meshing commands are replayed before it. What the
    /// replay meshes by is decided by the version the model was written by, which the session holds on to,
    /// and not by the items; the items are what make the mesh survive a save by this version. Placing it in
    /// front of the meshing instead would not do: a model built by a script carries no meshing command at
    /// all, since the whole script is recorded as one command.
    /// </summary>
    [Serializable]
    class CAddNetgenMeshToUncoveredParts : PreprocessCommand
    {
        // Variables
        [OptionalField]
        private NetgenMesh[] _meshSetupItems;


        // Constructor
        public CAddNetgenMeshToUncoveredParts()
            : base("Add Netgen mesh")
        {
            _meshSetupItems = null;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            _meshSetupItems = receiver.AddNetgenMeshesToUncoveredParts();
            return true;
        }
        // The items are built from the parts the model was left with, so only the items added speak. The
        // journal exporter writes them before the first meshing command, since the model was meshed by them.
        public override void ToPython(PythonJournalWriter writer)
        {
            if (_meshSetupItems == null)
            {
                writer.Unsupported(this, "the command has not run in this session, so the mesh setup items it " +
                                         "adds are not known; regenerate the model and export the journal again");
                return;
            }
            //
            if (_meshSetupItems.Length == 0)
            {
                writer.Comment(Name + ": every part already has a mesh setup item");
                writer.CountEmittedCommand();
            }
            foreach (var meshSetupItem in _meshSetupItems) PythonMeshSetupEmitter.Emit(writer, null, meshSetupItem);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Parts without a mesh setup item";
        }
    }
}
