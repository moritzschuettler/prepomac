// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Linq;

namespace APIs.Python
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
        [PythonAPI("The mesh-computed part volume, or a CAD value only when the mesh had no value.", Returns = "float")]
        public double volume { get { return ResolvePart().MassProperties.Volume; } }
        [PythonAPI("The total surface area. A mesh computes an area only for a shell part: a solid " +
                   "carries the area of the CAD part it was meshed from and reports 0 when there is none. " +
                   "The area of the mesh itself is the sum of get_face_data().areas.", Returns = "float")]
        public double surface_area { get { return ResolvePart().MassProperties.Area; } }
        [PythonAPI("The mesh-computed center of mass or area centroid.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object center_of_mass
        {
            get
            {
                double[] center = ResolvePart().MassProperties.CenterOfMass;
                return NumPyConverter.ToPythonArray(center == null ? new double[0] : center);
            }
        }


        // Constructors
        internal PartAPI(string resultName, string partName, MeshDataService meshService)
        {
            _resultName = resultName;
            _partName = partName;
            _meshService = meshService;
        }


        // Methods
        [PythonAPI("Returns the node IDs in this part.", Returns = "numpy.ndarray or IntList")]
        public object get_node_ids()
        {
            return NumPyConverter.ToPythonArray(_meshService.GetPartData(_resultName, _partName).NodeIds);
        }
        [PythonAPI("Returns the element IDs in this part.", Returns = "numpy.ndarray or IntList")]
        public object get_element_ids()
        {
            return NumPyConverter.ToPythonArray(_meshService.GetPartData(_resultName, _partName).ElementIds);
        }
        [PythonAPI("Returns the result mesh part bounding box as [xMin, xMax, yMin, yMax, zMin, zMax].",
                   Returns = "NumericArray")]
        public NumericArrayAPI get_bounding_box()
        {
            BoundingBox boundingBox = ResolvePart().BoundingBox;
            if (boundingBox == null) return new NumericArrayAPI(new double[0]);
            return new NumericArrayAPI(new double[] { boundingBox.MinX, boundingBox.MaxX, boundingBox.MinY,
                                                      boundingBox.MaxY, boundingBox.MinZ, boundingBox.MaxZ });
        }
        [PythonAPI("Returns result-mesh faces with selectable IDs, mesh area, centroid, normal and geometric type.",
                   Returns = "FaceData")]
        public Model.FaceDataAPI get_face_data()
        {
            return Model.PartTopologyReader.GetFaceData(_meshService.GetMeshObject(_resultName), ResolvePart());
        }
        [PythonAPI("Returns result-mesh edges with selectable IDs, mesh length, centroid and geometric type.",
                   Returns = "EdgeData")]
        public Model.EdgeDataAPI get_edge_data()
        {
            return Model.PartTopologyReader.GetEdgeData(_meshService.GetMeshObject(_resultName), ResolvePart());
        }
        [PythonAPI("Returns result-mesh vertices with their node IDs, geometry IDs and coordinates.",
                   Returns = "VertexData")]
        public Model.VertexDataAPI get_vertex_data()
        {
            return Model.PartTopologyReader.GetVertexData(_meshService.GetMeshObject(_resultName), ResolvePart());
        }
        [PythonAPI("Returns the undeformed coordinates of the part nodes with shape (node_count, 3), in " +
                   "the order get_node_ids() reports them.", Returns = "numpy.ndarray or NumericArray")]
        public object get_node_coordinates()
        {
            return Model.PartTopologyReader.GetNodeCoordinates(
                       _meshService.ResolveResultWithMesh(_resultName).UndeformedNodes, ResolvePart());
        }
        [PythonAPI("Returns the 3 by 3 inertia matrix around the center of mass, or an empty array when " +
                   "unavailable. It is a second moment of volume: the density is 1, so the values are in " +
                   "model length units to the fifth power, and a mass inertia is this matrix multiplied by " +
                   "the material density. A mesh does not compute one, so it is the matrix of the CAD part " +
                   "the mesh came from, and an imported mesh has none.",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_inertia_at_center_of_mass()
        {
            return ToPythonMatrix(ResolvePart().MassProperties.InertiaMatrixCG);
        }
        [PythonAPI("Returns the 3 by 3 inertia matrix around the global origin, or an empty array when " +
                   "unavailable. It is a second moment of volume, with the same density of 1 and the same " +
                   "model length units to the fifth power as get_inertia_at_center_of_mass().",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_inertia_at_origin()
        {
            return ToPythonMatrix(ResolvePart().MassProperties.InertiaMatrixOrigin);
        }
        [PythonAPI("Returns supplemental CAD metadata transferred to the result mesh. Existing mesh measurements " +
                   "are omitted from metadata and remain available through the part, face and edge data.",
                   Returns = "GeometryMetadata")]
        public GeometryMetadataAPI get_geometry_metadata()
        {
            return new GeometryMetadataAPI(ResolvePart().GeometryMetadata);
        }
        private BasePart ResolvePart()
        {
            return _meshService.GetPartObject(_resultName, _partName);
        }
        private static object ToPythonMatrix(double[][] values)
        {
            if (values == null || values.Length != 3 || values.Any(row => row == null || row.Length != 3))
                return NumPyConverter.ToPythonArray(new double[0]);
            double[,] matrix = new double[3, 3];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++) matrix[row, column] = values[row][column];
            }
            return NumPyConverter.ToPythonArray(matrix);
        }
        public override string ToString()
        {
            PartData data = _meshService.GetPartData(_resultName, _partName);
            return "Part(name='" + _partName + "', nodes=" + data.NodeIds.Length + ", elements=" + data.ElementIds.Length + ")";
        }
    }
}
