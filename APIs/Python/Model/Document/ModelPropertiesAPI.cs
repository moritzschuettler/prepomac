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
    public class ModelPropertiesAPI : BaseAPI
    {
        // Variables
        private readonly DocumentService _service;


        // Properties
        [PythonAPI("The model name.", Returns = "str")]
        public string name
        {
            get { return _service.GetModelName(); }
            set { _service.SetModelName(value); }
        }
        [PythonAPI("The model space: ThreeD, PlaneStress, PlaneStrain or Axisymmetric.", Returns = "str")]
        public string model_space
        {
            get { return _service.GetModelSpace(); }
            set { _service.SetModelSpaceAndUnitSystem(value, null); }
        }
        [PythonAPI("The unit system: M_KG_S_C, MM_TON_S_C, M_TON_S_C, IN_LB_S_F or UNIT_LESS.", Returns = "str")]
        public string unit_system
        {
            get { return _service.GetUnitSystem(); }
            set { _service.SetModelSpaceAndUnitSystem(null, value); }
        }


        // Constructors
        internal ModelPropertiesAPI(DocumentService service)
        {
            _service = service;
        }


        // Methods
        public override string ToString()
        {
            return "ModelProperties(name=" + StringListAPI.ToDisplayString(_service.GetModelName()) +
                   ", model_space=" + StringListAPI.ToDisplayString(_service.GetModelSpace()) +
                   ", unit_system=" + StringListAPI.ToDisplayString(_service.GetUnitSystem()) + ")";
        }
    }
}
