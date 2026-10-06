// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class NodeSetAPI : BaseAPI
    {
        // Variables
        private readonly string _nodeSetName;
        private readonly NodeSetService _service;


        // Properties
        [PythonAPI("The node set name.", Returns = "str")]
        public string name { get { return _service.ResolveNodeSet(_nodeSetName).Name; } }
        [PythonAPI("The number of nodes in the node set.", Returns = "int")]
        public int count
        {
            get
            {
                int[] labels = _service.ResolveNodeSet(_nodeSetName).Labels;
                return labels == null ? 0 : labels.Length;
            }
        }
        [PythonAPI("True when the node set was resolved successfully during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveNodeSet(_nodeSetName).Valid; } }
        [PythonAPI("True when the node set was created internally for another model item.", Returns = "bool")]
        public bool is_internal { get { return _service.ResolveNodeSet(_nodeSetName).Internal; } }


        // Constructors
        internal NodeSetAPI(string nodeSetName, NodeSetService service)
        {
            _nodeSetName = nodeSetName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the node IDs of the node set.", Returns = "IntList")]
        public IntListAPI get_node_ids()
        {
            int[] labels = _service.ResolveNodeSet(_nodeSetName).Labels;
            return new IntListAPI(labels == null ? new int[0] : labels);
        }

        [PythonAPI("Returns the center of gravity of the node set as [x, y, z], or an empty list when it has " +
                   "none.", Returns = "list[float]")]
        public object get_center_of_gravity()
        {
            FeNodeSet nodeSet = _service.ResolveNodeSet(_nodeSetName);
            return PythonListConverter.ToPythonList(nodeSet.CenterOfGravity);
        }
        public override string ToString()
        {
            return "NodeSet(name=" + StringListAPI.ToDisplayString(_nodeSetName) + ", count=" + count + ")";
        }
    }
}
