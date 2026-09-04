// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    // The geometry branch of the model tree
    public class GeometryAPI : BaseAPI
    {
        // Variables
        private readonly GeometryPartsAPI _parts;


        // Properties
        [PythonAPI("The geometry parts of the model.", Returns = "GeometryParts")]
        public GeometryPartsAPI parts { get { return _parts; } }


        // Constructors
        internal GeometryAPI(GeometryPartService geometryPartService)
        {
            _parts = new GeometryPartsAPI(geometryPartService);
        }


        // Methods
        public override string ToString()
        {
            return "Geometry(parts=" + _parts.Count + ")";
        }
    }
}
