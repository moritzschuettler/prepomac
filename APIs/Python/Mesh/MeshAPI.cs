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

namespace APIs.Python
{
    public class MeshAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;
        private readonly NodesAPI _nodes;
        private readonly ElementsAPI _elements;
        private readonly PartsAPI _parts;
        private readonly ResultNodeSetsAPI _nodeSets;
        private readonly ResultElementSetsAPI _elementSets;
        private readonly ResultSurfacesAPI _surfaces;


        // Properties
        [PythonAPI("The result mesh nodes.", Returns = "Nodes")]
        public NodesAPI nodes { get { return _nodes; } }
        [PythonAPI("The result mesh elements.", Returns = "Elements")]
        public ElementsAPI elements { get { return _elements; } }
        [PythonAPI("The result mesh parts.", Returns = "Parts")]
        public PartsAPI parts { get { return _parts; } }
        [PythonAPI("The user-visible, read-only result node sets.", Returns = "NodeSets")]
        public ResultNodeSetsAPI node_sets { get { return _nodeSets; } }
        [PythonAPI("The user-visible, read-only result element sets.", Returns = "ElementSets")]
        public ResultElementSetsAPI element_sets { get { return _elementSets; } }
        [PythonAPI("The user-visible, read-only result surfaces.", Returns = "Surfaces")]
        public ResultSurfacesAPI surfaces { get { return _surfaces; } }


        // Constructors
        internal MeshAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
            _nodes = new NodesAPI(_resultName, _meshService);
            _elements = new ElementsAPI(_resultName, _meshService);
            _parts = new PartsAPI(_resultName, _meshService);
            _nodeSets = new ResultNodeSetsAPI(_resultName, _meshService);
            _elementSets = new ResultElementSetsAPI(_resultName, _meshService);
            _surfaces = new ResultSurfacesAPI(_resultName, _meshService);
        }


        // Methods
        public override string ToString()
        {
            CaeResults.FeResults result = _meshService.ResolveResultWithMesh(_resultName);
            return "Mesh(nodes=" + result.Mesh.Nodes.Count + ", elements=" + result.Mesh.Elements.Count +
                   ", parts=" + result.Mesh.Parts.Count +
                   ", node_sets=" + _meshService.GetNodeSetNames(_resultName).Length +
                   ", element_sets=" + _meshService.GetElementSetNames(_resultName).Length +
                   ", surfaces=" + _meshService.GetSurfaceNames(_resultName).Length + ")";
        }
    }
}
