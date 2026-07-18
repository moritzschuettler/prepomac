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
    public class MeshAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;
        private readonly NodesAPI _nodes;
        private readonly ElementsAPI _elements;
        private readonly PartsAPI _parts;


        // Properties
        [PythonAPI("The result mesh nodes.", Returns = "Nodes")]
        public NodesAPI nodes { get { return _nodes; } }
        [PythonAPI("The result mesh elements.", Returns = "Elements")]
        public ElementsAPI elements { get { return _elements; } }
        [PythonAPI("The result mesh parts.", Returns = "Parts")]
        public PartsAPI parts { get { return _parts; } }


        // Constructors
        internal MeshAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
            _nodes = new NodesAPI(_resultName, _meshService);
            _elements = new ElementsAPI(_resultName, _meshService);
            _parts = new PartsAPI(_resultName, _meshService);
        }


        // Methods
        public override string ToString()
        {
            CaeResults.FeResults result = _meshService.ResolveResultWithMesh(_resultName);
            return "Mesh(nodes=" + result.Mesh.Nodes.Count + ", elements=" + result.Mesh.Elements.Count +
                   ", parts=" + result.Mesh.Parts.Count + ")";
        }
    }
}
