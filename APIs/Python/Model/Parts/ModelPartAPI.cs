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
    public class ModelPartAPI : PartAPI
    {
        // Variables
        private readonly string _partName;
        private readonly ModelPartService _service;


        // Constructors
        internal ModelPartAPI(string partName, ModelPartService service)
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
