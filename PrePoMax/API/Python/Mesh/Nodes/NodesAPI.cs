// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class NodesAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Properties
        [PythonAPI("The number of result mesh nodes.", Returns = "int")]
        public int count { get { return _meshService.ResolveResultWithMesh(_resultName).Mesh.Nodes.Count; } }


        // Constructors
        internal NodesAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        [PythonAPI("Returns node IDs and undeformed coordinates in one bulk extraction.", Returns = "NodeData")]
        public NodeDataAPI get_data()
        {
            return _meshService.GetNodeData(_resultName);
        }

        [PythonAPI("Returns all node IDs.", Returns = "numpy.ndarray or IntList")]
        public object get_ids()
        {
            return get_data().ids;
        }

        [PythonAPI("Returns all undeformed node coordinates with shape (node_count, 3).", Returns = "numpy.ndarray or NumericArray" )]
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
