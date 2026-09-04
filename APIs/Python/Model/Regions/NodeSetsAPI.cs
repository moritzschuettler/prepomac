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

namespace APIs.Python.Model
{
    public class NodeSetsAPI : ModelItemsAPI<FeNodeSet, NodeSetAPI>
    {
        // Variables
        private readonly NodeSetService _service;


        // Constructors
        internal NodeSetsAPI(NodeSetService service)
            : base("node set")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a node set from a selection and adds it to the model using command history.",
                   Arguments = "name: str, selection: Selection", Returns = "NodeSet",
                   Example = "pmx.model.node_sets.add('Fixed', selection)")]
        public NodeSetAPI add(string name, SelectionAPI selection)
        {
            _service.Add(name, selection);
            return this[name];
        }

        [PythonAPI("Replaces an existing node set using command history.",
                   Arguments = "old_name: str, name: str, selection: Selection", Returns = "NodeSet")]
        public NodeSetAPI replace(string old_name, string name, SelectionAPI selection)
        {
            _service.Replace(old_name, name, selection);
            return this[name];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override FeNodeSet GetItem(string name)
        {
            return _service.GetNodeSet(name);
        }
        protected override NodeSetAPI CreateItemAPI(string name)
        {
            return new NodeSetAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(FeNodeSet item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "NodeSets(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
