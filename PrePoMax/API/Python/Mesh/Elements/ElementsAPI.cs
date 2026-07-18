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
    public class ElementsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Properties
        [PythonAPI("The number of result mesh elements.", Returns = "int")]
        public int count { get { return _meshService.ResolveResultWithMesh(_resultName).Mesh.Elements.Count; } }


        // Constructors
        internal ElementsAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public ElementAPI __getitem__(int elementId)
        {
            return this[elementId];
        }
        public ElementAPI this[int elementId]
        {
            get { return _meshService.GetElement(_resultName, elementId); }
        }
        public int __len__()
        {
            return count;
        }
        public bool __contains__(int elementId)
        {
            return contains(elementId);
        }

        [PythonAPI("Returns element IDs, offsets, node counts, and flattened connectivity arrays.", Returns = "ElementData")]
        public ElementDataAPI get_data()
        {
            return _meshService.GetElementData(_resultName);
        }

        [PythonAPI("Returns all element IDs.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return new IntListAPI(_meshService.GetElementIds(_resultName));
        }

        [PythonAPI("Returns the first mesh element.", Returns = "Element")]
        public ElementAPI first()
        {
            int[] ids = _meshService.GetElementIds(_resultName);
            if (ids.Length == 0) throw new PythonAPIException("The elements collection is empty.");
            return this[ids[0]];
        }

        [PythonAPI("Returns the last mesh element.", Returns = "Element")]
        public ElementAPI last()
        {
            int[] ids = _meshService.GetElementIds(_resultName);
            if (ids.Length == 0) throw new PythonAPIException("The elements collection is empty.");
            return this[ids[ids.Length - 1]];
        }

        [PythonAPI("Returns true if the element ID exists.", Arguments = "element_id: int", Returns = "bool")]
        public bool contains(int elementId)
        {
            return _meshService.GetElementIds(_resultName).Contains(elementId);
        }

        [PythonAPI("Returns node IDs for the specified element ID.", Arguments = "element_id: int", Returns = "IntList")]
        public IntListAPI get_node_ids(int elementId)
        {
            return new IntListAPI(_meshService.GetElementInfo(_resultName, elementId).NodeIds);
        }
        public override string ToString()
        {
            return "Elements(count=" + count + ")";
        }
    }
}
