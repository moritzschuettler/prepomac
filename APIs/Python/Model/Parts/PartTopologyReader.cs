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
using System.Collections.Generic;

namespace APIs.Python.Model
{
    // The faces, edges and vertices of a part, read off the visualization the application already
    // keeps for picking. Nothing here is computed that the application does not compute for itself:
    // the areas, lengths and entity types are the stored ones. Geometry uses exact CAD centroids when
    // metadata provides them; otherwise a centroid is the mean of the entity node coordinates.
    //
    // The reported IDs are the ones a selection takes. A face and an edge ID are 1-based within the
    // part, the way Query displays them, and a vertex is named by its node ID; the geometry ID of
    // each is reported beside it, because that is what a recorded selection and the session journal
    // carry.
    internal static class PartTopologyReader
    {
        // Static methods
        public static FaceDataAPI GetFaceData(FeMesh mesh, BasePart part)
        {
            VisualizationData visualization = part.Visualization;
            int count = visualization == null || !HasSelectableSurfaces(part) ? 0 : visualization.FaceCount;
            int[] ids = new int[count];
            int[] geometryIds = new int[count];
            double[] areas = new double[count];
            double[,] centroids = new double[count, 3];
            double[,] normals = new double[count, 3];
            string[] types = new string[count];
            if (count == 0) return new FaceDataAPI(ids, geometryIds, areas, centroids, normals, types);
            //
            int typeId = (int)SelectionService.GetSurfaceGeometryType(part);
            Dictionary<int, HashSet<int>> faceIdNodeIds = visualization.GetSurfaceIdNodeIds();
            for (int i = 0; i < count; i++)
            {
                ids[i] = i + 1;
                geometryIds[i] = FeMesh.GetGeometryId(i, typeId, part.PartId);
                areas[i] = visualization.FaceAreas != null && i < visualization.FaceAreas.Length
                           ? visualization.FaceAreas[i] : 0;
                types[i] = visualization.FaceTypes != null && i < visualization.FaceTypes.Length
                           ? visualization.FaceTypes[i].ToString() : GeomFaceType.Unknown.ToString();
                HashSet<int> nodeIds;
                if (!WriteGeometryCentroid(part, "face", i + 1, centroids, i) &&
                    faceIdNodeIds.TryGetValue(i, out nodeIds))
                    WriteCentroid(mesh, nodeIds, centroids, i);
                WriteFaceNormal(mesh, visualization, i, normals);
            }
            //
            return new FaceDataAPI(ids, geometryIds, areas, centroids, normals, types);
        }
        public static EdgeDataAPI GetEdgeData(FeMesh mesh, BasePart part)
        {
            VisualizationData visualization = part.Visualization;
            int count = visualization == null || visualization.EdgeCellIdsByEdge == null ? 0 : visualization.EdgeCount;
            int[] ids = new int[count];
            int[] geometryIds = new int[count];
            double[] lengths = new double[count];
            double[,] centroids = new double[count, 3];
            string[] types = new string[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = i + 1;
                geometryIds[i] = FeMesh.GetGeometryId(i, (int)GeometryType.Edge, part.PartId);
                lengths[i] = visualization.EdgeLengths != null && i < visualization.EdgeLengths.Length
                             ? visualization.EdgeLengths[i] : 0;
                types[i] = visualization.EdgeTypes != null && i < visualization.EdgeTypes.Length
                           ? visualization.EdgeTypes[i].ToString() : GeomCurveType.Unknown.ToString();
                if (!WriteGeometryCentroid(part, "edge", i + 1, centroids, i))
                    WriteCentroid(mesh, visualization.GetNodeIdsForEdgeId(i), centroids, i);
            }
            //
            return new EdgeDataAPI(ids, geometryIds, lengths, centroids, types);
        }
        public static VertexDataAPI GetVertexData(FeMesh mesh, BasePart part)
        {
            VisualizationData visualization = part.Visualization;
            int[] vertexNodeIds = visualization == null ? null : visualization.VertexNodeIds;
            int count = vertexNodeIds == null ? 0 : vertexNodeIds.Length;
            int[] nodeIds = new int[count];
            int[] geometryIds = new int[count];
            double[,] coordinates = new double[count, 3];
            for (int i = 0; i < count; i++)
            {
                nodeIds[i] = vertexNodeIds[i];
                geometryIds[i] = FeMesh.GetGeometryId(i, (int)GeometryType.Vertex, part.PartId);
                FeNode node;
                if (mesh != null && mesh.Nodes.TryGetValue(vertexNodeIds[i], out node))
                {
                    coordinates[i, 0] = node.X;
                    coordinates[i, 1] = node.Y;
                    coordinates[i, 2] = node.Z;
                }
            }
            //
            return new VertexDataAPI(nodeIds, geometryIds, coordinates);
        }
        // The nodes are taken by label rather than from the mesh order, so the rows line up with the
        // part node IDs. A result passes its undeformed nodes, which is what its node data reports
        public static object GetNodeCoordinates(Dictionary<int, FeNode> nodes, BasePart part)
        {
            int[] nodeLabels = part.NodeLabels;
            int count = nodeLabels == null ? 0 : nodeLabels.Length;
            double[,] coordinates = new double[count, 3];
            for (int i = 0; i < count; i++)
            {
                FeNode node;
                if (nodes == null || !nodes.TryGetValue(nodeLabels[i], out node)) continue;
                coordinates[i, 0] = node.X;
                coordinates[i, 1] = node.Y;
                coordinates[i, 2] = node.Z;
            }
            //
            return NumPyConverter.ToPythonArray(coordinates);
        }
        // A wire part has no faces, and the surfaces of a compound are picked on its sub parts, so
        // neither reports faces rather than failing the whole query
        private static bool HasSelectableSurfaces(BasePart part)
        {
            return part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell ||
                   part.PartType == PartType.Shell;
        }
        private static void WriteCentroid(FeMesh mesh, HashSet<int> nodeIds, double[,] centroids, int row)
        {
            if (mesh == null || nodeIds == null || nodeIds.Count == 0) return;
            //
            double[] sum = new double[3];
            int used = 0;
            foreach (int nodeId in nodeIds)
            {
                FeNode node;
                if (!mesh.Nodes.TryGetValue(nodeId, out node)) continue;
                sum[0] += node.X;
                sum[1] += node.Y;
                sum[2] += node.Z;
                used++;
            }
            if (used == 0) return;
            //
            for (int i = 0; i < 3; i++) centroids[row, i] = sum[i] / used;
        }
        private static bool WriteGeometryCentroid(BasePart part, string kind, int id, double[,] centroids, int row)
        {
            GeometryMetadataEntity entity = part.GeometryMetadata?.GetEntity(kind, id);
            double[] values;
            if (entity == null || !entity.TryGetCanonicalNumbers("centroid", out values) || values.Length != 3) return false;
            for (int i = 0; i < 3; i++) centroids[row, i] = values[i];
            return true;
        }
        // The mean of the cell normals of the face. A planar face has one normal and this is it; a
        // curved one has no single normal, and the mean is the direction it faces on average
        private static void WriteFaceNormal(FeMesh mesh, VisualizationData visualization, int faceId,
                                            double[,] normals)
        {
            if (mesh == null || visualization.CellIdsByFace == null ||
                faceId >= visualization.CellIdsByFace.Length) return;
            //
            int[] cellIds = visualization.CellIdsByFace[faceId];
            if (cellIds == null || cellIds.Length == 0) return;
            //
            double[] sum = new double[3];
            int used = 0;
            for (int i = 0; i < cellIds.Length; i++)
            {
                int[] cell = visualization.Cells[cellIds[i]];
                FeNode normal;
                try
                {
                    normal = mesh.ComputeNormalFromFaceCellIndices(cell);
                }
                catch (Exception)
                {
                    // A cell shape the normal computation does not support leaves the others counted
                    continue;
                }
                sum[0] += normal.X;
                sum[1] += normal.Y;
                sum[2] += normal.Z;
                used++;
            }
            if (used == 0) return;
            //
            double length = Math.Sqrt(sum[0] * sum[0] + sum[1] * sum[1] + sum[2] * sum[2]);
            if (length <= 0) return;
            //
            for (int i = 0; i < 3; i++) normals[faceId, i] = sum[i] / length;
        }
    }
}
