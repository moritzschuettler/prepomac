// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeMesh;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FileInOut.Input
{
    internal static class GeometryMetadataReader
    {
        // Methods
        public static GeometryMetadata Read(string data)
        {
            if (String.IsNullOrEmpty(data)) return null;
            string[] lines = data.Split(new string[] { "\r\n", "\n", "\r" },
                                        StringSplitOptions.None);
            int version = 0;
            string coordinateSystem = null;
            string lengthUnit = null;
            GeometryMetadataEntity current = null;
            string currentLabel = null;
            List<GeometryMetadataEntity> entities = new List<GeometryMetadataEntity>();
            bool inGeometry = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("VIS file format:", StringComparison.Ordinal))
                {
                    string value = line.Substring("VIS file format:".Length).Trim();
                    Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out version);
                }
                else if (line.StartsWith("Coordinate system:", StringComparison.Ordinal))
                    coordinateSystem = line.Substring("Coordinate system:".Length).Trim();
                else if (line.StartsWith("Length unit:", StringComparison.Ordinal))
                    lengthUnit = line.Substring("Length unit:".Length).Trim();
                else if (line == "Geometry") inGeometry = true;
                else if (line == "Visualization")
                {
                    if (current != null) entities.Add(current);
                    current = null;
                    break;
                }
                else if (inGeometry)
                {
                    string kind;
                    string label;
                    int id;
                    if (TryReadEntityHeader(line, out kind, out label, out id))
                    {
                        if (current != null) entities.Add(current);
                        current = new GeometryMetadataEntity(kind, id);
                        currentLabel = label;
                    }
                    else if (current != null && line.StartsWith(currentLabel + " ", StringComparison.Ordinal))
                    {
                        int colon = line.IndexOf(':');
                        if (colon < 0) continue;
                        string propertyName = line.Substring(currentLabel.Length + 1,
                                                             colon - currentLabel.Length - 1).Trim();
                        string valueText = line.Substring(colon + 1).Trim();
                        string[] values = valueText.Split(new char[] { ' ', '\t' },
                                                          StringSplitOptions.RemoveEmptyEntries);
                        double[] numbers;
                        GeometryMetadataProperty property = TryParseNumbers(values, out numbers)
                                                          ? new GeometryMetadataProperty(numbers)
                                                          : new GeometryMetadataProperty(values);
                        current.Properties[propertyName] = property;
                    }
                }
            }
            if (version != 2 || entities.Count == 0) return null;
            //
            GeometryMetadata metadata = new GeometryMetadata(version, coordinateSystem, lengthUnit);
            foreach (GeometryMetadataEntity entity in entities) metadata.AddEntity(entity);
            return metadata;
        }
        private static bool TryReadEntityHeader(string line, out string kind, out string label, out int id)
        {
            string[] labels = new string[] { "Solid", "Shell", "Face", "Edge", "Vertex" };
            foreach (string oneLabel in labels)
            {
                string prefix = oneLabel + " number:";
                if (!line.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string value = line.Substring(prefix.Length).Trim();
                if (Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                {
                    kind = oneLabel.ToLowerInvariant();
                    label = oneLabel;
                    return true;
                }
            }
            kind = null;
            label = null;
            id = 0;
            return false;
        }
        public static GeometryMetadata CreatePartMetadata(GeometryMetadata source,
                                                           Dictionary<int, int> localFaceIdSourceFaceId,
                                                           Dictionary<int, int> localEdgeIdSourceEdgeId)
        {
            if (source == null) return null;
            Dictionary<int, int> sourceFaceIdLocalFaceId = InvertAndMakeOneBased(localFaceIdSourceFaceId);
            Dictionary<int, int> sourceEdgeIdLocalEdgeId = InvertAndMakeOneBased(localEdgeIdSourceEdgeId);
            GeometryMetadata part = new GeometryMetadata(source.FormatVersion, source.CoordinateSystem,
                                                         source.LengthUnit);
            // Faces and edges use the same 1-based IDs exposed by the selection and Python APIs.
            AddMappedEntities(source, part, "face", localFaceIdSourceFaceId, sourceEdgeIdLocalEdgeId,
                              "edge_ids");
            AddMappedEntities(source, part, "edge", localEdgeIdSourceEdgeId, sourceFaceIdLocalFaceId,
                              "face_ids");
            // OCC vertex IDs cannot be mapped reliably to the merged visualization nodes. Preserve their
            // provenance explicitly instead of presenting them as selectable part vertex IDs.
            foreach (GeometryMetadataEntity edge in part.GetEntities("edge"))
            {
                GeometryMetadataProperty vertexIds;
                if (edge.Properties.TryGetValue("vertex_ids", out vertexIds))
                {
                    edge.Properties.Remove("vertex_ids");
                    edge.Properties["source_vertex_ids"] = vertexIds;
                }
            }
            // Shells and solids are retained when they contain at least one face of this part.
            Dictionary<int, int> sourceShellIdLocalShellId =
                AddContainers(source, part, "shell", sourceFaceIdLocalFaceId, null);
            AddContainers(source, part, "solid", sourceFaceIdLocalFaceId, sourceShellIdLocalShellId);
            return part.IsEmpty ? null : part;
        }
        public static void ApplyCanonicalMeasurements(BasePart part)
        {
            GeometryMetadata metadata = part?.GeometryMetadata;
            VisualizationData visualization = part?.Visualization;
            if (metadata == null || visualization == null) return;
            //
            GeometryMetadataEntity[] faces = metadata.GetEntities("face");
            GeometryMetadataEntity[] edges = metadata.GetEntities("edge");
            if (visualization.FaceAreas != null)
            {
                foreach (GeometryMetadataEntity face in faces)
                {
                    double[] values;
                    int index = face.Id - 1;
                    if (index >= 0 && index < visualization.FaceAreas.Length &&
                        face.TryGetCanonicalNumbers("area", out values) && values.Length == 1)
                        visualization.FaceAreas[index] = values[0];
                }
            }
            if (visualization.EdgeLengths != null)
            {
                foreach (GeometryMetadataEntity edge in edges)
                {
                    double[] values;
                    int index = edge.Id - 1;
                    if (index >= 0 && index < visualization.EdgeLengths.Length &&
                        edge.TryGetCanonicalNumbers("length", out values) && values.Length == 1)
                        visualization.EdgeLengths[index] = values[0];
                }
            }
            ApplyBoundingBox(part, faces.Length > 0 ? faces : edges);
            //
            GeometryMetadataEntity[] solids = metadata.GetEntities("solid");
            if (solids.Length > 0) ApplySolidMassProperties(part, solids);
            else ApplyShellMassProperties(part, metadata.GetEntities("shell"));
        }
        private static bool TryParseNumbers(string[] values, out double[] numbers)
        {
            numbers = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!Double.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    numbers = null;
                    return false;
                }
            }
            return true;
        }
        private static void ApplyBoundingBox(BasePart part, GeometryMetadataEntity[] entities)
        {
            double[] bounds = null;
            foreach (GeometryMetadataEntity entity in entities)
            {
                double[] oneBounds;
                if (!entity.TryGetNumbers("bounding_box", out oneBounds) || oneBounds.Length != 6 ||
                    oneBounds.Any(value => Double.IsNaN(value) || Double.IsInfinity(value)) ||
                    oneBounds[0] > oneBounds[1] || oneBounds[2] > oneBounds[3] ||
                    oneBounds[4] > oneBounds[5]) continue;
                if (bounds == null) bounds = oneBounds.ToArray();
                else
                {
                    bounds[0] = Math.Min(bounds[0], oneBounds[0]);
                    bounds[1] = Math.Max(bounds[1], oneBounds[1]);
                    bounds[2] = Math.Min(bounds[2], oneBounds[2]);
                    bounds[3] = Math.Max(bounds[3], oneBounds[3]);
                    bounds[4] = Math.Min(bounds[4], oneBounds[4]);
                    bounds[5] = Math.Max(bounds[5], oneBounds[5]);
                }
            }
            if (bounds != null) part.BoundingBox = new BoundingBox(bounds);
        }
        private static Dictionary<int, int> InvertAndMakeOneBased(Dictionary<int, int> localIdSourceId)
        {
            Dictionary<int, int> sourceIdLocalId = new Dictionary<int, int>();
            if (localIdSourceId == null) return sourceIdLocalId;
            foreach (var entry in localIdSourceId) sourceIdLocalId[entry.Value] = entry.Key + 1;
            return sourceIdLocalId;
        }
        private static void AddMappedEntities(GeometryMetadata source, GeometryMetadata part, string kind,
                                              Dictionary<int, int> localIdSourceId,
                                              Dictionary<int, int> linkedSourceIdLocalId,
                                              string linkedPropertyName)
        {
            if (localIdSourceId == null) return;
            foreach (var entry in localIdSourceId.OrderBy(item => item.Key))
            {
                GeometryMetadataEntity sourceEntity = source.GetEntity(kind, entry.Value);
                if (sourceEntity == null) continue;
                GeometryMetadataEntity localEntity = CopyWithId(sourceEntity, entry.Key + 1);
                localEntity.Properties["source_id"] =
                    new GeometryMetadataProperty(new double[] { sourceEntity.Id });
                RemapIds(localEntity, linkedPropertyName, linkedSourceIdLocalId);
                part.AddEntity(localEntity);
            }
        }
        private static Dictionary<int, int> AddContainers(GeometryMetadata source, GeometryMetadata part,
                                                           string kind,
                                                           Dictionary<int, int> sourceFaceIdLocalFaceId,
                                                           Dictionary<int, int> sourceShellIdLocalShellId)
        {
            Dictionary<int, int> sourceIdLocalId = new Dictionary<int, int>();
            foreach (GeometryMetadataEntity sourceEntity in source.GetEntities(kind))
            {
                double[] faceIds;
                if (!sourceEntity.TryGetNumbers("face_ids", out faceIds) ||
                    !faceIds.Any(id => sourceFaceIdLocalFaceId.ContainsKey((int)id))) continue;
                int localId = sourceIdLocalId.Count + 1;
                sourceIdLocalId.Add(sourceEntity.Id, localId);
                GeometryMetadataEntity localEntity = CopyWithId(sourceEntity, localId);
                localEntity.Properties["source_id"] =
                    new GeometryMetadataProperty(new double[] { sourceEntity.Id });
                RemapIds(localEntity, "face_ids", sourceFaceIdLocalFaceId);
                if (sourceShellIdLocalShellId != null)
                    RemapIds(localEntity, "shell_ids", sourceShellIdLocalShellId);
                part.AddEntity(localEntity);
            }
            return sourceIdLocalId;
        }
        private static GeometryMetadataEntity CopyWithId(GeometryMetadataEntity source, int id)
        {
            GeometryMetadataEntity copy = new GeometryMetadataEntity(source.Kind, id);
            foreach (var entry in source.Properties) copy.Properties.Add(entry.Key, entry.Value.DeepCopy());
            return copy;
        }
        private static void RemapIds(GeometryMetadataEntity entity, string propertyName,
                                     Dictionary<int, int> sourceIdLocalId)
        {
            double[] sourceIds;
            if (sourceIdLocalId == null || !entity.TryGetNumbers(propertyName, out sourceIds)) return;
            double[] localIds = sourceIds.Where(id => sourceIdLocalId.ContainsKey((int)id))
                                         .Select(id => (double)sourceIdLocalId[(int)id]).ToArray();
            entity.Properties[propertyName] = new GeometryMetadataProperty(localIds);
        }
        private static void ApplySolidMassProperties(BasePart part, GeometryMetadataEntity[] solids)
        {
            double volume = 0;
            double area = 0;
            double[] firstMoment = new double[3];
            double[,] inertiaAtOrigin = new double[3, 3];
            foreach (GeometryMetadataEntity solid in solids)
            {
                double[] oneVolume;
                double[] center;
                double[] inertia;
                if (!solid.TryGetCanonicalNumbers("volume", out oneVolume) || oneVolume.Length != 1 ||
                    !solid.TryGetCanonicalNumbers("center_of_mass", out center) || center.Length != 3) continue;
                double value = oneVolume[0];
                volume += value;
                for (int i = 0; i < 3; i++) firstMoment[i] += value * center[i];
                double[] oneArea;
                if (solid.TryGetCanonicalNumbers("surface_area", out oneArea) && oneArea.Length == 1) area += oneArea[0];
                if (solid.TryGetCanonicalNumbers("inertia_at_center_of_mass", out inertia) && inertia.Length == 9)
                {
                    double radiusSquared = center[0] * center[0] + center[1] * center[1] + center[2] * center[2];
                    for (int row = 0; row < 3; row++)
                    {
                        for (int column = 0; column < 3; column++)
                        {
                            double shift = value * ((row == column ? radiusSquared : 0) -
                                                    center[row] * center[column]);
                            inertiaAtOrigin[row, column] += inertia[3 * row + column] + shift;
                        }
                    }
                }
            }
            if (volume <= 0) return;
            //
            PartMassProperties properties = new PartMassProperties(true);
            properties.Volume = volume;
            properties.Area = area;
            for (int i = 0; i < 3; i++) properties.CenterOfMass[i] = firstMoment[i] / volume;
            double centerRadiusSquared = properties.CenterOfMass.Sum(value => value * value);
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    properties.InertiaMatrixOrigin[row][column] = inertiaAtOrigin[row, column];
                    double shift = volume * ((row == column ? centerRadiusSquared : 0) -
                                             properties.CenterOfMass[row] * properties.CenterOfMass[column]);
                    properties.InertiaMatrixCG[row][column] = inertiaAtOrigin[row, column] - shift;
                }
            }
            part.MassProperties = properties;
        }
        private static void ApplyShellMassProperties(BasePart part, GeometryMetadataEntity[] shells)
        {
            double area = 0;
            double[] firstMoment = new double[3];
            foreach (GeometryMetadataEntity shell in shells)
            {
                double[] oneArea;
                double[] centroid;
                if (!shell.TryGetCanonicalNumbers("area", out oneArea) || oneArea.Length != 1 ||
                    !shell.TryGetCanonicalNumbers("centroid", out centroid) || centroid.Length != 3) continue;
                area += oneArea[0];
                for (int i = 0; i < 3; i++) firstMoment[i] += oneArea[0] * centroid[i];
            }
            if (area <= 0) return;
            //
            PartMassProperties properties = part.MassProperties;
            properties.Area = area;
            properties.CenterOfMass = firstMoment.Select(value => value / area).ToArray();
            part.MassProperties = properties;
        }
    }
}
