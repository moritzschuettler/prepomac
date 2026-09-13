// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;

namespace CaeMesh
{
    [Serializable]
    public class GeometryMetadataEntity
    {
        // Variables
        private readonly string _kind;
        private readonly int _id;
        private readonly Dictionary<string, GeometryMetadataProperty> _properties;


        // Properties
        public string Kind { get { return _kind; } }
        public int Id { get { return _id; } }
        public Dictionary<string, GeometryMetadataProperty> Properties { get { return _properties; } }


        // Constructors
        public GeometryMetadataEntity(string kind, int id)
        {
            _kind = kind;
            _id = id;
            _properties = new Dictionary<string, GeometryMetadataProperty>(StringComparer.OrdinalIgnoreCase);
        }
        public GeometryMetadataEntity(GeometryMetadataEntity entity)
            : this(entity.Kind, entity.Id)
        {
            foreach (var entry in entity.Properties) _properties.Add(entry.Key, entry.Value.DeepCopy());
        }


        // Methods
        public GeometryMetadataEntity DeepCopy()
        {
            return new GeometryMetadataEntity(this);
        }
        public bool TryGetNumbers(string propertyName, out double[] values)
        {
            GeometryMetadataProperty property;
            if (_properties.TryGetValue(propertyName, out property) && property.IsNumeric)
            {
                values = property.NumericValues;
                return true;
            }
            values = null;
            return false;
        }
        public bool TryGetText(string propertyName, out string[] values)
        {
            GeometryMetadataProperty property;
            if (_properties.TryGetValue(propertyName, out property) && !property.IsNumeric)
            {
                values = property.TextValues;
                return true;
            }
            values = null;
            return false;
        }
        public void FillMissingFrom(GeometryMetadataEntity source, ISet<string> excludedProperties = null)
        {
            foreach (var entry in source.Properties)
            {
                if (excludedProperties != null && excludedProperties.Contains(entry.Key)) continue;
                if (!_properties.ContainsKey(entry.Key)) _properties.Add(entry.Key, entry.Value.DeepCopy());
            }
        }
    }
}
