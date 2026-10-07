// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace CaeMesh
{
    [Serializable]
    public class GeometryMetadata : ISerializable, IDeserializationCallback
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
        // The entities are saved as one block of bytes. Saved one object at a time, the faces and edges of an assembly
        // are hundreds of thousands of objects, and the binary formatter takes seconds to copy or save them
        protected GeometryMetadata(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_formatVersion":
                        _formatVersion = (int)entry.Value; break;
                    case "_coordinateSystem":
                        _coordinateSystem = (string)entry.Value; break;
                    case "_lengthUnit":
                        _lengthUnit = (string)entry.Value; break;
                    case "_entityData":
                        _entities = ReadEntities((byte[])entry.Value); break;
                    case "_entities":
                        // Compatibility for version v2.6.0
                        _entities = (Dictionary<string, List<GeometryMetadataEntity>>)entry.Value; break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }


        // Methods
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_formatVersion", _formatVersion, typeof(int));
            info.AddValue("_coordinateSystem", _coordinateSystem, typeof(string));
            info.AddValue("_lengthUnit", _lengthUnit, typeof(string));
            info.AddValue("_entityData", WriteEntities(), typeof(byte[]));
        }
        public void OnDeserialization(object sender)
        {
            // An entity dictionary saved as an object is filled only once the formatter calls it back
            ((IDeserializationCallback)_entities).OnDeserialization(sender);
        }
        private byte[] WriteEntities()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(_entities.Count);
                foreach (var entry in _entities)
                {
                    bw.Write(entry.Key);
                    bw.Write(entry.Value.Count);
                    foreach (GeometryMetadataEntity entity in entry.Value)
                    {
                        bw.Write(entity.Kind);
                        bw.Write(entity.Id);
                        bw.Write(entity.Properties.Count);
                        foreach (var property in entity.Properties)
                        {
                            bw.Write(property.Key);
                            bw.Write(property.Value.IsNumeric);
                            if (property.Value.IsNumeric)
                            {
                                bw.Write(property.Value.NumericValues.Length);
                                foreach (double value in property.Value.NumericValues) bw.Write(value);
                            }
                            else
                            {
                                bw.Write(property.Value.TextValues.Length);
                                foreach (string value in property.Value.TextValues)
                                {
                                    bw.Write(value != null);
                                    if (value != null) bw.Write(value);
                                }
                            }
                        }
                    }
                }
                bw.Flush();
                return stream.ToArray();
            }
        }
        private static Dictionary<string, List<GeometryMetadataEntity>> ReadEntities(byte[] data)
        {
            Dictionary<string, List<GeometryMetadataEntity>> entities =
                new Dictionary<string, List<GeometryMetadataEntity>>(StringComparer.OrdinalIgnoreCase);
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(stream))
            {
                int numOfKinds = br.ReadInt32();
                for (int i = 0; i < numOfKinds; i++)
                {
                    string kind = br.ReadString();
                    int numOfEntities = br.ReadInt32();
                    List<GeometryMetadataEntity> kindEntities = new List<GeometryMetadataEntity>(numOfEntities);
                    for (int j = 0; j < numOfEntities; j++)
                    {
                        string entityKind = br.ReadString();
                        GeometryMetadataEntity entity = new GeometryMetadataEntity(entityKind, br.ReadInt32());
                        int numOfProperties = br.ReadInt32();
                        for (int k = 0; k < numOfProperties; k++)
                        {
                            string name = br.ReadString();
                            bool numeric = br.ReadBoolean();
                            int numOfValues = br.ReadInt32();
                            GeometryMetadataProperty property;
                            if (numeric)
                            {
                                double[] values = new double[numOfValues];
                                for (int l = 0; l < numOfValues; l++) values[l] = br.ReadDouble();
                                property = new GeometryMetadataProperty(values);
                            }
                            else
                            {
                                string[] values = new string[numOfValues];
                                for (int l = 0; l < numOfValues; l++) values[l] = br.ReadBoolean() ? br.ReadString() : null;
                                property = new GeometryMetadataProperty(values);
                            }
                            entity.Properties.Add(name, property);
                        }
                        kindEntities.Add(entity);
                    }
                    entities.Add(kind, kindEntities);
                }
            }
            return entities;
        }
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
