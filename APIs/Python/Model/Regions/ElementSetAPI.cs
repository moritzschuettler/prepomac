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
    public class ElementSetAPI : BaseAPI
    {
        // Variables
        private readonly string _elementSetName;
        private readonly ElementSetService _service;


        // Properties
        [PythonAPI("The element set name.", Returns = "str")]
        public string name { get { return _service.ResolveElementSet(_elementSetName).Name; } }
        [PythonAPI("The number of elements in the element set.", Returns = "int")]
        public int count
        {
            get
            {
                int[] labels = _service.ResolveElementSet(_elementSetName).Labels;
                return labels == null ? 0 : labels.Length;
            }
        }
        [PythonAPI("True when the element set resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveElementSet(_elementSetName).Valid; } }
        [PythonAPI("True when the element set was created internally for another model item.", Returns = "bool")]
        public bool is_internal { get { return _service.ResolveElementSet(_elementSetName).Internal; } }


        // Constructors
        internal ElementSetAPI(string elementSetName, ElementSetService service)
        {
            _elementSetName = elementSetName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the element IDs of the element set.", Returns = "IntList")]
        public IntListAPI get_element_ids()
        {
            int[] labels = _service.ResolveElementSet(_elementSetName).Labels;
            return new IntListAPI(labels == null ? new int[0] : labels);
        }
        public override string ToString()
        {
            return "ElementSet(name=" + StringListAPI.ToDisplayString(_elementSetName) + ", count=" + count + ")";
        }
    }
}
