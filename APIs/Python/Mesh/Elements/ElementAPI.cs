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
    public class ElementAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _elementId;
        private readonly MeshDataService _meshService;


        // Properties
        [PythonAPI("The element ID.", Returns = "int")]
        public int id { get { return Info.Id; } }
        [PythonAPI("The PrePoMax element type name.", Returns = "str")]
        public string type_name { get { return Info.TypeName; } }
        [PythonAPI("The element dimension.", Returns = "int")]
        public int dimension { get { return Info.Dimension; } }
        [PythonAPI("The number of element nodes.", Returns = "int")]
        public int node_count { get { return Info.NodeIds.Length; } }

        private ElementInfo Info { get { return _meshService.GetElementInfo(_resultName, _elementId); } }


        // Constructors
        internal ElementAPI(string resultName, int elementId, MeshDataService meshService)
        {
            _resultName = resultName;
            _elementId = elementId;
            _meshService = meshService;
        }


        // Methods
        [PythonAPI("Returns the element node IDs.", Returns = "IntList")]
        public IntListAPI get_node_ids()
        {
            return new IntListAPI(Info.NodeIds);
        }
        public override string ToString()
        {
            ElementInfo info = Info;
            return "Element(id=" + info.Id + ", type='" + info.TypeName + "', nodes=" + info.NodeIds.Length + ")";
        }
    }
}
