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
    // The edges of one part, as parallel arrays in edge order. The IDs are what a selection takes:
    // 'ids' with the 'edges' source, 'geometry_ids' with the 'geometry' source.
    public class EdgeDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly IntListAPI _ids;
        private readonly IntListAPI _geometryIds;
        private readonly object _lengths;
        private readonly object _centroids;
        private readonly StringListAPI _types;


        // Properties
        [PythonAPI("The number of edges.", Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The 1-based edge IDs of the part, as the 'edges' selection source takes them.",
                   Returns = "IntList")]
        public IntListAPI ids { get { return _ids; } }
        [PythonAPI("The geometry ID of every edge, as the 'geometry' selection source takes them.",
                   Returns = "IntList")]
        public IntListAPI geometry_ids { get { return _geometryIds; } }
        [PythonAPI("The canonical length of every edge, in model units. Geometry parts use exact CAD values; " +
                   "model and result parts keep their mesh-computed values.", Returns = "numpy.ndarray or NumericArray")]
        public object lengths { get { return _lengths; } }
        [PythonAPI("The centroid of every edge with shape (edge_count, 3). Geometry parts use the exact CAD " +
                   "centroid when available; mesh parts use the mean of their node coordinates.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object centroids { get { return _centroids; } }
        [PythonAPI("The geometric type of every edge: Line, Circle, Ellipse, Hyperbola, Parabola, " +
                   "BezierCurve, BSplineCurve, OffsetCurve, OtherCurve or Unknown.", Returns = "StringList")]
        public StringListAPI types { get { return _types; } }


        // Constructors
        internal EdgeDataAPI(int[] ids, int[] geometryIds, double[] lengths, double[,] centroids, string[] types)
        {
            _count = ids == null ? 0 : ids.Length;
            _ids = new IntListAPI(ids);
            _geometryIds = new IntListAPI(geometryIds);
            _lengths = NumPyConverter.ToPythonArray(lengths);
            _centroids = NumPyConverter.ToPythonArray(centroids);
            _types = new StringListAPI(types);
        }


        // Methods
        public override string ToString()
        {
            return "EdgeData(count=" + _count + ")";
        }
    }
}
