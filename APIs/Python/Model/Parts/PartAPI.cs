// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Linq;

namespace APIs.Python.Model
{
    // The read-only members shared by a geometry part and a model part. A part is changed through its
    // collection, because that is what the command history records.
    public abstract class PartAPI : BaseAPI
    {
        // Properties
        [PythonAPI("The part name.", Returns = "str")]
        public string name { get { return ResolvePart().Name; } }
        [PythonAPI("The part ID.", Returns = "int")]
        public int id { get { return ResolvePart().PartId; } }
        [PythonAPI("The part type: Solid, SolidAsShell, Shell, Wire, Compound or Unknown.", Returns = "str")]
        public string part_type { get { return ResolvePart().PartType.ToString(); } }
        [PythonAPI("The number of nodes in the part.", Returns = "int")]
        public int node_count
        {
            get
            {
                int[] nodeLabels = ResolvePart().NodeLabels;
                return nodeLabels == null ? 0 : nodeLabels.Length;
            }
        }
        [PythonAPI("The number of elements in the part.", Returns = "int")]
        public int element_count
        {
            get
            {
                int[] labels = ResolvePart().Labels;
                return labels == null ? 0 : labels.Length;
            }
        }
        [PythonAPI("The part color as [R, G, B, A] channel values.", Returns = "IntList")]
        public IntListAPI color { get { return PythonColorConverter.ToChannels(ResolvePart().Color); } }
        [PythonAPI("True when the part is visible.", Returns = "bool")]
        public bool visible { get { return ResolvePart().Visible; } }
        [PythonAPI("True when the part is active.", Returns = "bool")]
        public bool active { get { return ResolvePart().Active; } }
        [PythonAPI("The canonical part volume. CAD geometry uses the exact Netgen value; model meshes use " +
                   "their mesh-computed value.", Returns = "float")]
        public double volume { get { return ResolvePart().MassProperties.Volume; } }
        [PythonAPI("The total surface area. CAD geometry uses the exact Netgen value. A mesh computes an " +
                   "area only for a shell part: a meshed solid carries the area of the CAD part it was " +
                   "meshed from and reports 0 when there is none, as an imported mesh has none. The area " +
                   "of the mesh itself is the sum of get_face_data().areas.", Returns = "float")]
        public double surface_area { get { return ResolvePart().MassProperties.Area; } }
        [PythonAPI("The canonical center of mass or area centroid as [x, y, z].", Returns = "list[float]")]
        public object center_of_mass
        {
            get { return PythonListConverter.ToPythonList(ResolvePart().MassProperties.CenterOfMass); }
        }


        // Methods
        [PythonAPI("Returns the part bounding box as [xMin, xMax, yMin, yMax, zMin, zMax].",
                   Returns = "list[float]")]
        public object get_bounding_box()
        {
            BoundingBox boundingBox = ResolvePart().BoundingBox;
            if (boundingBox == null) return PythonListConverter.ToPythonList(new double[0]);
            //
            return PythonListConverter.ToPythonList(new double[] { boundingBox.MinX, boundingBox.MaxX,
                                                                   boundingBox.MinY, boundingBox.MaxY,
                                                                   boundingBox.MinZ, boundingBox.MaxZ });
        }

        [PythonAPI("Returns the node IDs of the part.", Returns = "IntList")]
        public IntListAPI get_node_ids()
        {
            int[] nodeLabels = ResolvePart().NodeLabels;
            return new IntListAPI(nodeLabels == null ? new int[0] : nodeLabels);
        }

        [PythonAPI("Returns the coordinates of the part nodes with shape (node_count, 3), in the order " +
                   "get_node_ids() reports them. This is the route to the nodes of a geometry part, " +
                   "because pmx.model.nodes is the model mesh and holds nothing until the model is meshed.",
                   Returns = "numpy.ndarray or NumericArray",
                   Example = "ids = part.get_node_ids()\n" +
                             "coordinates = part.get_node_coordinates()")]
        public object get_node_coordinates()
        {
            return PartTopologyReader.GetNodeCoordinates(ResolveMesh().Nodes, ResolvePart());
        }

        [PythonAPI("Returns the element IDs of the part.", Returns = "IntList")]
        public IntListAPI get_element_ids()
        {
            int[] labels = ResolvePart().Labels;
            return new IntListAPI(labels == null ? new int[0] : labels);
        }

        [PythonAPI("Returns the faces of the part with the IDs a selection takes, and with the area, the " +
                   "centroid, the normal and the geometric type of each one. This is how a script finds the " +
                   "face it means without picking it in the viewport.", Returns = "FaceData",
                   Example = "faces = part.get_face_data()\n" +
                             "top = int(faces.ids[list(faces.normals[:, 2]).index(max(faces.normals[:, 2]))])")]
        public FaceDataAPI get_face_data()
        {
            return PartTopologyReader.GetFaceData(ResolveMesh(), ResolvePart());
        }

        [PythonAPI("Returns the edges of the part with the IDs a selection takes, and with the length, the " +
                   "centroid and the geometric type of each one.", Returns = "EdgeData")]
        public EdgeDataAPI get_edge_data()
        {
            return PartTopologyReader.GetEdgeData(ResolveMesh(), ResolvePart());
        }

        [PythonAPI("Returns the vertices of the part with their node IDs, geometry IDs and coordinates.",
                   Returns = "VertexData")]
        public VertexDataAPI get_vertex_data()
        {
            return PartTopologyReader.GetVertexData(ResolveMesh(), ResolvePart());
        }

        [PythonAPI("Returns the 3 by 3 inertia matrix around the center of mass as three rows, or an empty " +
                   "list when unavailable. It is a second moment of volume: the density is 1, so the values " +
                   "are in model length units to the fifth power, and a mass inertia is this matrix " +
                   "multiplied by the material density. Only CAD geometry computes it; a meshed part " +
                   "carries the matrix of the CAD part it was meshed from, and an imported mesh has none.",
                   Returns = "list[list[float]]")]
        public object get_inertia_at_center_of_mass()
        {
            return ToPythonMatrix(ResolvePart().MassProperties.InertiaMatrixCG);
        }

        [PythonAPI("Returns the 3 by 3 inertia matrix around the global origin as three rows, or an empty " +
                   "list when unavailable. It is a second moment of volume, with the same density of 1 and " +
                   "the same model length units to the fifth power as get_inertia_at_center_of_mass().",
                   Returns = "list[list[float]]")]
        public object get_inertia_at_origin()
        {
            return ToPythonMatrix(ResolvePart().MassProperties.InertiaMatrixOrigin);
        }

        [PythonAPI("Returns detailed supplemental CAD entity metadata such as analytic axes, primitive " +
                   "parameters, orientation, bounds and topology links. Measurements already exposed by the " +
                   "part, face or edge API are omitted.", Returns = "GeometryMetadata")]
        public GeometryMetadataAPI get_geometry_metadata()
        {
            return new GeometryMetadataAPI(ResolvePart().GeometryMetadata);
        }
        //
        protected abstract BasePart ResolvePart();
        protected abstract FeMesh ResolveMesh();
        private static object ToPythonMatrix(double[][] values)
        {
            if (values == null || values.Length != 3 || values.Any(row => row == null || row.Length != 3))
                return PythonListConverter.ToPythonList(new double[0]);
            return PythonListConverter.ToPythonList(values);
        }
        //
        public override string ToString()
        {
            BasePart part = ResolvePart();
            return GetType().Name.Replace("API", "") + "(name=" + StringListAPI.ToDisplayString(part.Name) +
                   ", type=" + part.PartType + ", nodes=" + node_count + ", elements=" + element_count + ")";
        }
    }
}
