// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeMesh;
using System;
using System.Linq;

namespace APIs.Python
{
    public class GeometryMetadataEntityAPI : BaseAPI
    {
        // Variables
        private readonly GeometryMetadataEntity _entity;


        // Properties
        [PythonAPI("The entity kind: solid, shell, face or edge.", Returns = "str")]
        public string kind { get { return _entity.Kind; } }
        [PythonAPI("The 1-based entity ID within the part.", Returns = "int")]
        public int id { get { return _entity.Id; } }
        [PythonAPI("The available supplemental property names. Canonical area, length, volume, centroid and " +
                   "type values are exposed by the part topology properties instead.", Returns = "StringList")]
        public StringListAPI property_names
        {
            get
            {
                return new StringListAPI(_entity.Properties.Keys
                    .Where(name => !GeometryMetadata.IsCanonicalProperty(_entity.Kind, name)).OrderBy(name => name));
            }
        }


        // Constructors
        internal GeometryMetadataEntityAPI(GeometryMetadataEntity entity)
        {
            _entity = entity;
        }


        // Methods
        [PythonAPI("Returns a supplemental property, or the default when this entity does not carry it. " +
                   "The property set varies by entity type - a cylinder has a radius, a cone has a semi " +
                   "angle instead - so a caller reads what it wants without testing property_names first. " +
                   "Numeric vectors are returned as list[float], an ID list - a property whose name ends " +
                   "in _ids - as IntList and text vectors as StringList. A one-value property other than " +
                   "an ID list is returned as a scalar.",
                   Arguments = "name: str, default: object = None",
                   Returns = "float, str, list[float], IntList, StringList or the default",
                   Example = "radius = face.get_property('radius')   # None on a face that has no radius")]
        public object get_property(string name, object @default = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new PythonAPIException("The geometry metadata property name must not be empty.");
            if (GeometryMetadata.IsCanonicalProperty(_entity.Kind, name))
                throw new PythonAPIException("The property '" + name + "' is a canonical topology measurement " +
                                             "and is exposed directly by the part data API.");
            GeometryMetadataProperty property;
            if (!_entity.Properties.TryGetValue(name, out property)) return @default;
            if (property.IsNumeric)
            {
                double[] values = property.NumericValues;
                if (name.EndsWith("_ids", StringComparison.OrdinalIgnoreCase))
                    return new IntListAPI(values.Select(value => Convert.ToInt32(value)));
                if (values.Length == 1) return values[0];
                return PythonListConverter.ToPythonList(values);
            }
            else
            {
                string[] values = property.TextValues;
                if (values.Length == 1) return values[0];
                return new StringListAPI(values);
            }
        }
        public override string ToString()
        {
            return "GeometryMetadataEntity(kind=" + StringListAPI.ToDisplayString(_entity.Kind) +
                   ", id=" + _entity.Id + ", properties=" + property_names.count + ")";
        }
    }
}
