// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class GeometryPartAPI : PartAPI
    {
        // Variables
        private readonly string _partName;
        private readonly GeometryPartService _service;


        // Properties
        [PythonAPI("True when the part carries CAD data.", Returns = "bool")]
        public bool is_cad_part
        {
            get
            {
                GeometryPart part = ResolvePart() as GeometryPart;
                return part != null && part.IsCADPart;
            }
        }
        [PythonAPI("The most structured mesh the recognition finds the part supports, whatever the mesh setup " +
                   "items ask for: Transfinite, Sweep, Tetrahedral, or Other for a part that is not a CAD solid. " +
                   "A compound part reports the most restrictive type of its sub-parts, except that one in which " +
                   "some cells sweep and others do not reports Sweep, since the sweep meshes the cells it cannot " +
                   "reach by tetrahedra. The recognition depends " +
                   "on the geometry alone, so an exploded view does not change it.", Returns = "str")]
        public string mesh_type
        {
            get { return _service.GetRecognizedMeshType(_partName); }
        }


        // Constructors
        internal GeometryPartAPI(string partName, GeometryPartService service)
        {
            _partName = partName;
            _service = service;
        }


        // Methods
        protected override BasePart ResolvePart()
        {
            return _service.ResolvePart(_partName);
        }
        protected override FeMesh ResolveMesh()
        {
            return _service.ResolveMesh();
        }
    }
}
