// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    public class NodesAPI : BaseAPI
    {
        // Variables
        private readonly MeshEditService _service;


        // Properties
        [PythonAPI("The number of nodes in the model mesh.", Returns = "int")]
        public int count { get { return _service.GetNodeCount(); } }


        // Constructors
        internal NodesAPI(MeshEditService service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Renumbers the model nodes using command history.", Arguments = "start_id: int",
                   Returns = "None")]
        public void renumber(int start_id)
        {
            _service.RenumberNodes(start_id);
        }

        [PythonAPI("Merges the coincident nodes of the selected geometry using command history. " +
                   "The nodes to keep are SmallerID or LargerID.",
                   Arguments = "selection: Selection, tolerance: float = None, nodes_to_keep: str = None",
                   Returns = "None")]
        public void merge_coincident(SelectionAPI selection, object tolerance = null, string nodes_to_keep = null)
        {
            _service.MergeCoincidentNodes(selection, tolerance, nodes_to_keep);
        }
        public override string ToString()
        {
            return "Nodes(count=" + count + ")";
        }
    }
}
