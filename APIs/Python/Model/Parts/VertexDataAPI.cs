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
    // The vertices of one part, as parallel arrays in vertex order. A vertex is a node where more
    // than two edge cells meet, so it is named by its node ID: that is what the 'vertices' selection
    // source takes and what Query displays.
    public class VertexDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly object _nodeIds;
        private readonly object _geometryIds;
        private readonly object _coordinates;


        // Properties
        [PythonAPI("The number of vertices.", Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The node ID of every vertex, as the 'vertices' selection source takes them.",
                   Returns = "numpy.ndarray or IntList")]
        public object node_ids { get { return _nodeIds; } }
        [PythonAPI("The geometry ID of every vertex, as the 'geometry' selection source takes them.",
                   Returns = "numpy.ndarray or IntList")]
        public object geometry_ids { get { return _geometryIds; } }
        [PythonAPI("The coordinates of every vertex with shape (vertex_count, 3).",
                   Returns = "numpy.ndarray or NumericArray")]
        public object coordinates { get { return _coordinates; } }


        // Constructors
        internal VertexDataAPI(int[] nodeIds, int[] geometryIds, double[,] coordinates)
        {
            _count = nodeIds == null ? 0 : nodeIds.Length;
            _nodeIds = NumPyConverter.ToPythonArray(nodeIds);
            _geometryIds = NumPyConverter.ToPythonArray(geometryIds);
            _coordinates = NumPyConverter.ToPythonArray(coordinates);
        }


        // Methods
        public override string ToString()
        {
            return "VertexData(count=" + _count + ")";
        }
    }
}
