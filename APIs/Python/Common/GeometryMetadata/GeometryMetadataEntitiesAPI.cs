// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeMesh;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    public class GeometryMetadataEntitiesAPI : BaseAPI, IReadOnlyList<GeometryMetadataEntityAPI>
    {
        // Variables
        private readonly GeometryMetadataEntityAPI[] _entities;


        // Properties
        public int Count { get { return _entities.Length; } }
        [PythonAPI("The number of entities.", Returns = "int")]
        public int count { get { return _entities.Length; } }
        public GeometryMetadataEntityAPI this[int index]
        {
            get
            {
                if (index < 0) index += _entities.Length;
                if (index < 0 || index >= _entities.Length) throw new IndexOutOfRangeException();
                return _entities[index];
            }
        }


        // Constructors
        internal GeometryMetadataEntitiesAPI(IEnumerable<GeometryMetadataEntity> entities)
        {
            _entities = entities == null ? new GeometryMetadataEntityAPI[0]
                                         : entities.Select(item => new GeometryMetadataEntityAPI(item)).ToArray();
        }


        // Methods
        public int __len__()
        {
            return _entities.Length;
        }
        public GeometryMetadataEntityAPI __getitem__(int index)
        {
            return this[index];
        }
        [PythonAPI("Returns the entity with the specified 1-based ID.", Arguments = "id: int",
                   Returns = "GeometryMetadataEntity")]
        public GeometryMetadataEntityAPI get(object id)
        {
            int idValue = PythonValueConverter.ToInt(id, "id");
            GeometryMetadataEntityAPI entity = _entities.FirstOrDefault(item => item.id == idValue);
            if (entity == null)
                throw new PythonAPIException("Geometry metadata entity " + idValue + " does not exist.");
            return entity;
        }
        public IEnumerator<GeometryMetadataEntityAPI> GetEnumerator()
        {
            return ((IEnumerable<GeometryMetadataEntityAPI>)_entities).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entities.GetEnumerator();
        }
        public override string ToString()
        {
            return "GeometryMetadataEntities(count=" + _entities.Length + ")";
        }
    }
}
