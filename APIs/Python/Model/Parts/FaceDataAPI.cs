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
    // The faces of one part, as parallel arrays in face order. The IDs are what a selection takes:
    // 'ids' with the 'surfaces' source, 'geometry_ids' with the 'geometry' source.
    public class FaceDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly object _ids;
        private readonly object _geometryIds;
        private readonly object _areas;
        private readonly object _centroids;
        private readonly object _normals;
        private readonly StringListAPI _types;


        // Properties
        [PythonAPI("The number of faces.", Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The 1-based face IDs of the part, as the 'surfaces' selection source takes them.",
                   Returns = "numpy.ndarray or IntList")]
        public object ids { get { return _ids; } }
        [PythonAPI("The geometry ID of every face, as the 'geometry' selection source takes them.",
                   Returns = "numpy.ndarray or IntList")]
        public object geometry_ids { get { return _geometryIds; } }
        [PythonAPI("The canonical area of every face, in model units. Geometry parts use exact CAD values; " +
                   "model and result parts keep their mesh-computed values.", Returns = "numpy.ndarray or NumericArray")]
        public object areas { get { return _areas; } }
        [PythonAPI("The centroid of every face with shape (face_count, 3). Geometry parts use the exact CAD " +
                   "centroid when available; mesh parts use the mean of their node coordinates.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object centroids { get { return _centroids; } }
        [PythonAPI("The outward normal of every face with shape (face_count, 3), as the mean of its cell " +
                   "normals. A curved face has no single normal, so this is the direction it faces on average.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object normals { get { return _normals; } }
        [PythonAPI("The geometric type of every face: Plane, Cylinder, Cone, Sphere, Torus, BezierSurface, " +
                   "BSplineSurface, SurfaceOfRevolution, SurfaceOfExtrusion, OffsetSurface, OtherSurface or " +
                   "Unknown.", Returns = "StringList")]
        public StringListAPI types { get { return _types; } }


        // Constructors
        internal FaceDataAPI(int[] ids, int[] geometryIds, double[] areas, double[,] centroids,
                             double[,] normals, string[] types)
        {
            _count = ids == null ? 0 : ids.Length;
            _ids = NumPyConverter.ToPythonArray(ids);
            _geometryIds = NumPyConverter.ToPythonArray(geometryIds);
            _areas = NumPyConverter.ToPythonArray(areas);
            _centroids = NumPyConverter.ToPythonArray(centroids);
            _normals = NumPyConverter.ToPythonArray(normals);
            _types = new StringListAPI(types);
        }


        // Methods
        public override string ToString()
        {
            return "FaceData(count=" + _count + ")";
        }
    }
}
