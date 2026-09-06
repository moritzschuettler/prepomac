// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    public class MaterialAPI : BaseAPI
    {
        // Variables
        private readonly string _materialName;
        private readonly MaterialService _service;


        // Properties
        [PythonAPI("The material name.", Returns = "str")]
        public string name { get { return _service.ResolveMaterial(_materialName).Name; } }
        [PythonAPI("The material description.", Returns = "str")]
        public string description { get { return _service.ResolveMaterial(_materialName).Description; } }
        [PythonAPI("Whether every material-property row is temperature dependent.", Returns = "bool")]
        public bool temperature_dependent
        {
            get { return _service.ResolveMaterial(_materialName).TemperatureDependent; }
        }
        [PythonAPI("The number of material properties.", Returns = "int")]
        public int property_count { get { return _service.ResolveMaterial(_materialName).Properties.Count; } }


        // Constructors
        internal MaterialAPI(string materialName, MaterialService service)
        {
            _materialName = materialName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the type names of the material properties, for example Elastic or Density.",
                   Returns = "StringList")]
        public StringListAPI get_property_types()
        {
            List<MaterialProperty> properties = _service.ResolveMaterial(_materialName).Properties;
            return new StringListAPI(properties.Select(property => property.GetType().Name).ToArray());
        }
        [PythonAPI("Returns detached copies of all material properties, including their value tables.",
                   Returns = "list[MaterialProperty]")]
        public MaterialProperty[] get_properties()
        {
            Material material = _service.ResolveMaterial(_materialName).DeepClone();
            return material.Properties.ToArray();
        }
        public override string ToString()
        {
            Material material = _service.ResolveMaterial(_materialName);
            return "Material(name=" + StringListAPI.ToDisplayString(material.Name) +
                   ", properties=" + material.Properties.Count + ")";
        }
    }
}
