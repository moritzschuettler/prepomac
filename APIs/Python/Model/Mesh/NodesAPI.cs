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
        public void renumber(object start_id)
        {
            _service.RenumberNodes(PythonValueConverter.ToInt(start_id, "start_id"));
        }

        [PythonAPI("Merges the coincident nodes of the selected geometry or nodes using command history. " +
                   "The nodes to keep are SmallerID or LargerID.",
                   Arguments = "selection: Selection, tolerance: float = None, nodes_to_keep: str = None",
                   Returns = "None")]
        public void merge_coincident(object selection, object tolerance = null, object nodes_to_keep = null)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string nodesToKeep = PythonValueConverter.ToText(nodes_to_keep, "nodes_to_keep", true);
            _service.MergeCoincidentNodes(selectionValue, tolerance, nodesToKeep);
        }
        [PythonAPI("Returns a copied bulk mesh snapshot ordered by ID.", Returns = "NodeData")]
        public NodeDataAPI get_data()
        {
            return _service.GetNodeData();
        }
        [PythonAPI("Returns all model mesh IDs in ascending order.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return get_data().ids;
        }
        [PythonAPI("Returns model node coordinates with shape (node_count, 3).", Returns = "numpy.ndarray or NumericArray")]
        public object get_coordinates()
        {
            return get_data().coordinates;
        }
        public override string ToString()
        {
            return "Nodes(count=" + count + ")";
        }
    }
}
