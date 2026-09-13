// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeMesh
{
    [Serializable]
    public class GeometryMetadata
    {
        // Variables
        private readonly int _formatVersion;
        private readonly string _coordinateSystem;
        private readonly string _lengthUnit;
        private readonly Dictionary<string, List<GeometryMetadataEntity>> _entities;


        // Properties
        public int FormatVersion { get { return _formatVersion; } }
        public string CoordinateSystem { get { return _coordinateSystem; } }
        public string LengthUnit { get { return _lengthUnit; } }
        public string[] EntityKinds { get { return _entities.Keys.OrderBy(item => item).ToArray(); } }
        public bool IsEmpty { get { return _entities.Values.All(items => items.Count == 0); } }


        // Constructors
        public GeometryMetadata(int formatVersion, string coordinateSystem, string lengthUnit)
        {
            _formatVersion = formatVersion;
            _coordinateSystem = coordinateSystem;
            _lengthUnit = lengthUnit;
            _entities = new Dictionary<string, List<GeometryMetadataEntity>>(StringComparer.OrdinalIgnoreCase);
        }
        public GeometryMetadata(GeometryMetadata metadata)
            : this(metadata.FormatVersion, metadata.CoordinateSystem, metadata.LengthUnit)
        {
            foreach (string kind in metadata.EntityKinds)
            {
                foreach (GeometryMetadataEntity entity in metadata.GetEntities(kind)) AddEntity(entity.DeepCopy());
            }
        }


        // Methods
        public GeometryMetadata DeepCopy()
        {
            return new GeometryMetadata(this);
        }
        public void AddEntity(GeometryMetadataEntity entity)
        {
            List<GeometryMetadataEntity> entities;
            if (!_entities.TryGetValue(entity.Kind, out entities))
            {
                entities = new List<GeometryMetadataEntity>();
                _entities.Add(entity.Kind, entities);
            }
            int index = entities.FindIndex(item => item.Id == entity.Id);
            if (index >= 0) entities[index] = entity;
            else entities.Add(entity);
        }
        public GeometryMetadataEntity[] GetEntities(string kind)
        {
            List<GeometryMetadataEntity> entities;
            if (kind == null || !_entities.TryGetValue(kind, out entities)) return new GeometryMetadataEntity[0];
            return entities.OrderBy(item => item.Id).ToArray();
        }
        public GeometryMetadataEntity GetEntity(string kind, int id)
        {
            return GetEntities(kind).FirstOrDefault(item => item.Id == id);
        }
        public void FillMissingFrom(GeometryMetadata source, bool includeFaceEntities, bool includeEdgeEntities,
                                    bool omitCanonicalMeasurements)
        {
            if (source == null) return;
            foreach (string kind in source.EntityKinds)
            {
                if (kind.Equals("face", StringComparison.OrdinalIgnoreCase) && !includeFaceEntities) continue;
                if (kind.Equals("edge", StringComparison.OrdinalIgnoreCase) && !includeEdgeEntities) continue;
                if ((kind.Equals("solid", StringComparison.OrdinalIgnoreCase) ||
                     kind.Equals("shell", StringComparison.OrdinalIgnoreCase)) && !includeFaceEntities) continue;
                ISet<string> excluded = omitCanonicalMeasurements ? GetCanonicalProperties(kind) : null;
                foreach (GeometryMetadataEntity sourceEntity in source.GetEntities(kind))
                {
                    GeometryMetadataEntity targetEntity = GetEntity(kind, sourceEntity.Id);
                    if (targetEntity == null)
                    {
                        targetEntity = new GeometryMetadataEntity(kind, sourceEntity.Id);
                        AddEntity(targetEntity);
                    }
                    targetEntity.FillMissingFrom(sourceEntity, excluded);
                }
            }
        }
        public static bool IsCanonicalProperty(string kind, string propertyName)
        {
            return GetCanonicalProperties(kind).Contains(propertyName);
        }
        private static ISet<string> GetCanonicalProperties(string kind)
        {
            string[] names;
            if (kind.Equals("solid", StringComparison.OrdinalIgnoreCase))
                names = new string[] { "volume", "surface_area", "center_of_mass",
                                       "inertia_at_center_of_mass", "bounding_box" };
            else if (kind.Equals("shell", StringComparison.OrdinalIgnoreCase))
                names = new string[] { "area", "centroid", "bounding_box" };
            else if (kind.Equals("face", StringComparison.OrdinalIgnoreCase))
                names = new string[] { "type", "area", "centroid" };
            else if (kind.Equals("edge", StringComparison.OrdinalIgnoreCase))
                names = new string[] { "type", "length", "centroid" };
            else names = new string[0];
            return new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }
    }
}
