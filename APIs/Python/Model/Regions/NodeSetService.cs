// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System.Linq;

namespace APIs.Python.Model
{
    internal class NodeSetService : RegionService
    {
        // Constructors
        public NodeSetService(ModelResolver modelResolver, SelectionService selectionService,
                              IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) return new string[0];
            //
            return mesh.NodeSets.Keys.ToArray();
        }
        public FeNodeSet GetNodeSet(string nodeSetName)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || nodeSetName == null) return null;
            //
            FeNodeSet nodeSet;
            if (!mesh.NodeSets.TryGetValue(nodeSetName, out nodeSet)) return null;
            //
            return nodeSet;
        }
        public FeNodeSet ResolveNodeSet(string nodeSetName)
        {
            FeNodeSet nodeSet = GetNodeSet(nodeSetName);
            if (nodeSet == null)
                throw new PythonAPIException("The node set '" + nodeSetName + "' does not exist.");
            //
            return nodeSet;
        }
        public void Add(string name, SelectionAPI selection)
        {
            ValidateNewName(name, null);
            FeNodeSet nodeSet = Build(name, selection);
            // Conversion and validation are complete before the command gateway is called
            EnsureCommandsAvailable();
            ModelGateway.AddNodeSet(nodeSet);
        }
        public void Replace(string oldName, string name, SelectionAPI selection)
        {
            ResolveNodeSet(oldName);
            ValidateNewName(name, oldName);
            FeNodeSet nodeSet = Build(name, selection);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceNodeSet(oldName, nodeSet);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateNodeSets(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveNodeSets(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        private FeNodeSet Build(string name, SelectionAPI selection)
        {
            // The controller recomputes the labels from the creation data when the node set is added
            SelectionCompilation compilation = CompileModelSelection(selection, "nodes");
            FeNodeSet nodeSet = new FeNodeSet(name, compilation.Ids);
            nodeSet.CreationData = compilation.Selection;
            nodeSet.CreationIds = compilation.Ids;
            //
            return nodeSet;
        }
    }
}
