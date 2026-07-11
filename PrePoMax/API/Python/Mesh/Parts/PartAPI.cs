// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class PartAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _partName;
        private readonly MeshDataService _meshService;


        // Properties
        [PythonAPI("The result mesh part name.", Returns = "str")]
        public string name { get { return _partName; } }
        [PythonAPI("The node IDs in this part.", Returns = "numpy.ndarray or IntList")]
        public object node_ids { get { return NumPyConverter.ToPythonArray(_meshService.GetPartData(_resultName, _partName).NodeIds); } }
        [PythonAPI("The element IDs in this part.", Returns = "numpy.ndarray or IntList")]
        public object element_ids { get { return NumPyConverter.ToPythonArray(_meshService.GetPartData(_resultName, _partName).ElementIds); } }


        // Constructors
        internal PartAPI(string resultName, string partName, MeshDataService meshService)
        {
            _resultName = resultName;
            _partName = partName;
            _meshService = meshService;
        }


        // Methods
        public override string ToString()
        {
            PartData data = _meshService.GetPartData(_resultName, _partName);
            return "Part(name='" + _partName + "', nodes=" + data.NodeIds.Length + ", elements=" + data.ElementIds.Length + ")";
        }
    }
}
