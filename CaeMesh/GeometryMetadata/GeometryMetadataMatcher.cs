// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// Finds, for each face and edge of a mesh part, the face or edge of the geometry part it was meshed
    /// from, and gives the geometry metadata of those under the ids of the mesh part. The two are not
    /// numbered alike: a sub part of a compound is meshed with the faces of the compound, in the order
    /// the compound has them, while its geometry part numbers them in its own order. So they are paired
    /// by where they lie - the centroid, the size and, where both know it, the type - and not by number.
    /// </summary>
    public static class GeometryMetadataMatcher
    {
        // Methods
        public static GeometryMetadata Match(BasePart geometryPart, Dictionary<int, FeNode> geometryNodes,
                                             BasePart meshPart, Dictionary<int, FeNode> meshNodes)
        {
            GeometryMetadata source = geometryPart?.GeometryMetadata;
            VisualizationData geometryVisualization = geometryPart?.Visualization;
            VisualizationData meshVisualization = meshPart?.Visualization;
            if (source == null || geometryVisualization?.Cells == null || meshVisualization?.Cells == null ||
                geometryNodes == null || meshNodes == null) return null;
            //
            double tolerance = 0.02 * Math.Max(geometryPart.BoundingBox.GetDiagonal(), 1E-12);
            // [mesh id] -> geometry id, or -1
            int[] faceMap = MatchItems(GetFaceMeasures(geometryVisualization, geometryNodes),
                                       ToInts(geometryVisualization.FaceTypes),
                                       GetFaceMeasures(meshVisualization, meshNodes),
                                       ToInts(meshVisualization.FaceTypes),
                                       (int)GeomFaceType.Unknown, tolerance);
            int[] edgeMap = MatchItems(GetEdgeMeasures(geometryVisualization, geometryNodes),
                                       ToInts(geometryVisualization.EdgeTypes),
                                       GetEdgeMeasures(meshVisualization, meshNodes),
                                       ToInts(meshVisualization.EdgeTypes),
                                       (int)GeomCurveType.Unknown, tolerance);
            if (faceMap.All(id => id < 0) && edgeMap.All(id => id < 0)) return null;
            // The metadata names a geometry item by the id the kernel gave it, a mesh item by its place
            Dictionary<int, int> faceIds = GetMeshIdsByKernelId(faceMap, geometryVisualization.FaceKernelIds);
            Dictionary<int, int> edgeIds = GetMeshIdsByKernelId(edgeMap, geometryVisualization.EdgeKernelIds);
            //
            GeometryMetadata matched = new GeometryMetadata(source.FormatVersion, source.CoordinateSystem,
                                                            source.LengthUnit);
            AddMatchedEntities("face", faceMap, geometryVisualization.FaceKernelIds, source, faceIds, edgeIds,
                               matched);
            AddMatchedEntities("edge", edgeMap, geometryVisualization.EdgeKernelIds, source, faceIds, edgeIds,
                               matched);
            // A solid, a shell and a vertex keep their own ids and only name faces and edges
            foreach (string kind in new string[] { "solid", "shell", "vertex" })
            {
                foreach (GeometryMetadataEntity entity in source.GetEntities(kind))
                    matched.AddEntity(Renumber(entity, entity.Id, faceIds, edgeIds));
            }
            return matched;
        }
        private static int[] ToInts<T>(T[] values) where T : struct, IConvertible
        {
            if (values == null) return null;
            return values.Select(value => Convert.ToInt32(value)).ToArray();
        }
        // [item] -> { x, y, z, size }
        private static double[][] GetFaceMeasures(VisualizationData visualization, Dictionary<int, FeNode> nodes)
        {
            if (visualization.CellIdsByFace == null) return new double[0][];
            double[][] measures = new double[visualization.CellIdsByFace.Length][];
            for (int faceId = 0; faceId < measures.Length; faceId++)
            {
                double area = 0;
                double[] centroid = new double[3];
                foreach (int cellId in visualization.CellIdsByFace[faceId])
                {
                    int[] cell = visualization.Cells[cellId];
                    // A second order cell lists its mid side nodes after the corners
                    int numOfCorners = cell.Length == 6 ? 3 : cell.Length == 8 ? 4 : cell.Length;
                    for (int i = 1; i < numOfCorners - 1; i++)
                    {
                        double[] a = nodes[cell[0]].Coor;
                        double[] b = nodes[cell[i]].Coor;
                        double[] c = nodes[cell[i + 1]].Coor;
                        double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                        double[] ac = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                        double cx = ab[1] * ac[2] - ab[2] * ac[1];
                        double cy = ab[2] * ac[0] - ab[0] * ac[2];
                        double cz = ab[0] * ac[1] - ab[1] * ac[0];
                        double triangleArea = 0.5 * Math.Sqrt(cx * cx + cy * cy + cz * cz);
                        for (int k = 0; k < 3; k++) centroid[k] += triangleArea * (a[k] + b[k] + c[k]) / 3;
                        area += triangleArea;
                    }
                }
                if (area > 0) for (int k = 0; k < 3; k++) centroid[k] /= area;
                measures[faceId] = new double[] { centroid[0], centroid[1], centroid[2], area };
            }
            return measures;
        }
        private static double[][] GetEdgeMeasures(VisualizationData visualization, Dictionary<int, FeNode> nodes)
        {
            if (visualization.EdgeCellIdsByEdge == null || visualization.EdgeCells == null) return new double[0][];
            double[][] measures = new double[visualization.EdgeCellIdsByEdge.Length][];
            for (int edgeId = 0; edgeId < measures.Length; edgeId++)
            {
                double length = 0;
                double[] centroid = new double[3];
                foreach (int cellId in visualization.EdgeCellIdsByEdge[edgeId])
                {
                    int[] cell = visualization.EdgeCells[cellId];
                    double[] a = nodes[cell[0]].Coor;
                    double[] b = nodes[cell[1]].Coor;
                    double segmentLength = Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) +
                                                     (b[2] - a[2]) * (b[2] - a[2]));
                    for (int k = 0; k < 3; k++) centroid[k] += segmentLength * (a[k] + b[k]) / 2;
                    length += segmentLength;
                }
                if (length > 0) for (int k = 0; k < 3; k++) centroid[k] /= length;
                measures[edgeId] = new double[] { centroid[0], centroid[1], centroid[2], length };
            }
            return measures;
        }
        /// <summary>
        /// Pairs every target item with the nearest source item of a like size and type, the closest pairs
        /// first, so that one source item is never given to two targets. A target with no source within the
        /// tolerance is left without one.
        /// </summary>
        private static int[] MatchItems(double[][] sources, int[] sourceTypes, double[][] targets, int[] targetTypes,
                                        int unknownType, double tolerance)
        {
            List<Tuple<double, int, int>> pairs = new List<Tuple<double, int, int>>();
            for (int t = 0; t < targets.Length; t++)
            {
                for (int s = 0; s < sources.Length; s++)
                {
                    int sourceType = sourceTypes != null && s < sourceTypes.Length ? sourceTypes[s] : unknownType;
                    int targetType = targetTypes != null && t < targetTypes.Length ? targetTypes[t] : unknownType;
                    if (sourceType != unknownType && targetType != unknownType && sourceType != targetType) continue;
                    //
                    double sourceSize = sources[s][3];
                    double targetSize = targets[t][3];
                    if (sourceSize <= 0 || targetSize <= 0) continue;
                    double ratio = sourceSize / targetSize;
                    if (ratio < 0.8 || ratio > 1.25) continue;
                    //
                    double dx = sources[s][0] - targets[t][0];
                    double dy = sources[s][1] - targets[t][1];
                    double dz = sources[s][2] - targets[t][2];
                    double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (distance <= tolerance) pairs.Add(Tuple.Create(distance, t, s));
                }
            }
            int[] map = Enumerable.Repeat(-1, targets.Length).ToArray();
            HashSet<int> usedSources = new HashSet<int>();
            foreach (var pair in pairs.OrderBy(pair => pair.Item1))
            {
                if (map[pair.Item2] >= 0 || usedSources.Contains(pair.Item3)) continue;
                map[pair.Item2] = pair.Item3;
                usedSources.Add(pair.Item3);
            }
            return map;
        }
        private static int GetKernelId(int[] kernelIds, int id)
        {
            if (kernelIds != null && id < kernelIds.Length && kernelIds[id] > 0) return kernelIds[id];
            return id + 1;
        }
        private static Dictionary<int, int> GetMeshIdsByKernelId(int[] map, int[] kernelIds)
        {
            Dictionary<int, int> meshIds = new Dictionary<int, int>();
            for (int meshId = 0; meshId < map.Length; meshId++)
            {
                if (map[meshId] < 0) continue;
                int kernelId = GetKernelId(kernelIds, map[meshId]);
                if (!meshIds.ContainsKey(kernelId)) meshIds.Add(kernelId, meshId + 1);
            }
            return meshIds;
        }
        private static void AddMatchedEntities(string kind, int[] map, int[] kernelIds, GeometryMetadata source,
                                               Dictionary<int, int> faceIds, Dictionary<int, int> edgeIds,
                                               GeometryMetadata matched)
        {
            for (int meshId = 0; meshId < map.Length; meshId++)
            {
                if (map[meshId] < 0) continue;
                GeometryMetadataEntity entity = source.GetEntity(kind, GetKernelId(kernelIds, map[meshId]));
                if (entity != null) matched.AddEntity(Renumber(entity, meshId + 1, faceIds, edgeIds));
            }
        }
        /// <summary>
        /// A copy of the entity under a new id, with the faces and edges it names renumbered; one it names
        /// that the mesh part does not have is dropped.
        /// </summary>
        public static GeometryMetadataEntity Renumber(GeometryMetadataEntity entity, int id,
                                                      Dictionary<int, int> faceIds, Dictionary<int, int> edgeIds)
        {
            GeometryMetadataEntity renumbered = new GeometryMetadataEntity(entity.Kind, id);
            foreach (var entry in entity.Properties)
            {
                GeometryMetadataProperty property = entry.Value.DeepCopy();
                if (entry.Key.EndsWith("_ids"))
                {
                    Dictionary<int, int> ids = entry.Key == "face_ids" ? faceIds :
                                               entry.Key == "edge_ids" ? edgeIds : null;
                    if (ids == null || !property.IsNumeric) continue;
                    List<double> newIds = new List<double>();
                    foreach (double oldId in property.NumericValues)
                    {
                        int newId;
                        if (ids.TryGetValue((int)oldId, out newId)) newIds.Add(newId);
                    }
                    if (newIds.Count == 0) continue;
                    property = new GeometryMetadataProperty(newIds.ToArray());
                }
                renumbered.Properties.Add(entry.Key, property);
            }
            return renumbered;
        }
    }
}
