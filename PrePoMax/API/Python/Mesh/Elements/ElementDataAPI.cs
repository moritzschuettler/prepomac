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
    public class ElementDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly int[] _rawIds;
        private readonly int[] _rawOffsets;
        private readonly int[] _rawConnectivity;
        private readonly object _ids;
        private readonly object _offsets;
        private readonly object _nodeCounts;
        private readonly object _connectivity;


        // Properties
        [PythonAPI("Element IDs.", Returns = "numpy.ndarray or IntList")]
        public object ids { get { return _ids; } }
        [PythonAPI("Compressed connectivity offsets with length element_count + 1.", Returns = "numpy.ndarray or IntList")]
        public object offsets { get { return _offsets; } }
        [PythonAPI("Number of nodes for each element.", Returns = "numpy.ndarray or IntList")]
        public object node_counts { get { return _nodeCounts; } }
        [PythonAPI("Flattened node ID connectivity.", Returns = "numpy.ndarray or IntList")]
        public object connectivity { get { return _connectivity; } }
        [PythonAPI("The number of elements.", Returns = "int")]
        public int count { get { return _count; } }


        // Constructors
        internal ElementDataAPI(int[] ids, int[] offsets, int[] connectivity)
        {
            _count = ids != null ? ids.Length : 0;
            _rawIds = ids != null ? ids.ToArray() : new int[0];
            _rawOffsets = offsets != null ? offsets.ToArray() : new int[0];
            _rawConnectivity = connectivity != null ? connectivity.ToArray() : new int[0];
            int[] nodeCounts = new int[_count];
            for (int i = 0; i < _count; i++) nodeCounts[i] = _rawOffsets[i + 1] - _rawOffsets[i];
            _ids = NumPyConverter.ToPythonArray(_rawIds);
            _offsets = NumPyConverter.ToPythonArray(_rawOffsets);
            _nodeCounts = NumPyConverter.ToPythonArray(nodeCounts);
            _connectivity = NumPyConverter.ToPythonArray(_rawConnectivity);
        }


        // Methods
        [PythonAPI("Returns node IDs for the specified element ID.", Arguments = "element_id: int", Returns = "IntList")]
        public IntListAPI get_node_ids(int elementId)
        {
            return new IntListAPI(GetConnectivitySlice(elementId));
        }
        public override string ToString()
        {
            return "ElementData(count=" + _count + ")";
        }
        private int[] GetConnectivitySlice(int elementId)
        {
            int index = Array.IndexOf(_rawIds, elementId);
            if (index < 0) throw new PythonAPIException("The element ID " + elementId + " does not exist in this element data.");
            int start = _rawOffsets[index];
            int length = _rawOffsets[index + 1] - start;
            int[] nodeIds = new int[length];
            Array.Copy(_rawConnectivity, start, nodeIds, 0, length);
            return nodeIds;
        }
    }
}
