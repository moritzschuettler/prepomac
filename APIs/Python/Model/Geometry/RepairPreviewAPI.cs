// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace APIs.Python.Model
{
    // What a repair would take away from one part, found by running it on a copy: the edges it would remove or
    // sew, each as the points sampled on it, and the vertices it would remove.
    public class RepairPreviewAPI : BaseAPI
    {
        // Variables
        private readonly string _partName;
        private readonly bool _refused;
        private readonly int _edgeCount;
        private readonly object[] _edges;
        private readonly int _pointCount;
        private readonly object _points;


        // Properties
        [PythonAPI("The name of the part the repair would run on; a sub-part is repaired with its compound.",
                   Returns = "str")]
        public string part_name { get { return _partName; } }
        [PythonAPI("True when the modelling kernel would refuse the repair and leave the part as it was.",
                   Returns = "bool")]
        public bool refused { get { return _refused; } }
        [PythonAPI("The number of edges the repair would remove or sew.", Returns = "int")]
        public int edge_count { get { return _edgeCount; } }
        [PythonAPI("The edges the repair would remove or sew, each as the points sampled on it with shape " +
                   "(point_count, 3): the edges between fused faces, small edges, the edges of small faces and " +
                   "the free edges sewing would close.", Returns = "list[numpy.ndarray or NumericArray]")]
        public object[] edges { get { return _edges; } }
        [PythonAPI("The number of vertices the repair would remove.", Returns = "int")]
        public int point_count { get { return _pointCount; } }
        [PythonAPI("The vertices the repair would remove with shape (point_count, 3), such as the points joining " +
                   "edges takes out.", Returns = "numpy.ndarray or NumericArray")]
        public object points { get { return _points; } }


        // Constructors
        internal RepairPreviewAPI(string partName, bool refused, double[][][] edges, double[][] points)
        {
            _partName = partName;
            _refused = refused;
            _edgeCount = edges.Length;
            _edges = edges.Select(edge => NumPyConverter.ToPythonArray(ToMatrix(edge))).ToArray();
            _pointCount = points.Length;
            _points = NumPyConverter.ToPythonArray(ToMatrix(points));
        }


        // Methods
        private static double[,] ToMatrix(double[][] rows)
        {
            double[,] matrix = new double[rows.Length, 3];
            for (int i = 0; i < rows.Length; i++)
                for (int j = 0; j < 3; j++) matrix[i, j] = rows[i][j];
            return matrix;
        }
        public override string ToString()
        {
            return "RepairPreview(part_name=" + _partName + ", refused=" + (_refused ? "True" : "False") +
                   ", edge_count=" + _edgeCount + ", point_count=" + _pointCount + ")";
        }
    }
}
