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
    }
}
