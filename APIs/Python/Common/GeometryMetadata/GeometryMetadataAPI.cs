// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeMesh;
using System;

namespace APIs.Python
{
    public class GeometryMetadataAPI : BaseAPI
    {
        // Variables
        private readonly GeometryMetadata _metadata;


        // Properties
        [PythonAPI("True when detailed CAD metadata is available for this part.", Returns = "bool")]
        public bool available { get { return _metadata != null && !_metadata.IsEmpty; } }
        [PythonAPI("The Netgen geometry metadata format version, or 0 when unavailable.", Returns = "int")]
        public int format_version { get { return _metadata?.FormatVersion ?? 0; } }
        [PythonAPI("The metadata coordinate system, or None when unavailable.", Returns = "str or None")]
        public string coordinate_system { get { return _metadata?.CoordinateSystem; } }
        [PythonAPI("The metadata length unit declaration, or None when unavailable.", Returns = "str or None")]
        public string length_unit { get { return _metadata?.LengthUnit; } }
        [PythonAPI("The available entity kinds.", Returns = "StringList")]
        public StringListAPI entity_kinds
        {
            get { return new StringListAPI(_metadata?.EntityKinds ?? new string[0]); }
        }


        // Constructors
        internal GeometryMetadataAPI(GeometryMetadata metadata)
        {
            _metadata = metadata;
        }


        // Methods
        [PythonAPI("Returns the detailed entities of a kind. Unknown and unavailable kinds return an empty list.",
                   Arguments = "kind: str", Returns = "GeometryMetadataEntities")]
        public GeometryMetadataEntitiesAPI get_entities(string kind)
        {
            if (String.IsNullOrWhiteSpace(kind))
                throw new PythonAPIException("The geometry metadata entity kind must not be empty.");
            return new GeometryMetadataEntitiesAPI(_metadata?.GetEntities(kind) ??
                                                   new GeometryMetadataEntity[0]);
        }
        public override string ToString()
        {
            return "GeometryMetadata(available=" + available + ", kinds=" + entity_kinds.count + ")";
        }
    }
}
