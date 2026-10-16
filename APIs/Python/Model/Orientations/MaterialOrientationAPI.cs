// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class MaterialOrientationAPI : BaseAPI
    {
        // Variables
        private readonly string _materialOrientationName;
        private readonly MaterialOrientationService _service;


        // Properties
        [PythonAPI("The material orientation name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the region the orientation is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The region type the orientation is applied to.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the coordinate system whose axis is projected, Global for the global one.",
                   Returns = "str")]
        public string coordinate_system_name { get { return Resolve().CoordinateSystemName; } }
        [PythonAPI("The axis of the coordinate system that becomes the material axis 1: X, Y or Z.", Returns = "str")]
        public string reference_axis { get { return Resolve().ReferenceAxis.ToString(); } }
        [PythonAPI("The angle in degrees that turns the projected axis about the shell normal.", Returns = "float")]
        public double rotation_angle_deg { get { return Resolve().RotationAngle.Value; } }
        [PythonAPI("The density of the material axes symbols as a percentage of the full density.",
                   Returns = "int")]
        public int symbol_density { get { return Resolve().SymbolDensity; } }
        [PythonAPI("True when the orientation is active.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the orientation resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal MaterialOrientationAPI(string materialOrientationName, MaterialOrientationService service)
        {
            _materialOrientationName = materialOrientationName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the expression the rotation angle was entered with, without evaluating it, under " +
                   "rotation_angle_deg. Only a value entered as an expression is returned, with its leading equals " +
                   "sign; a value entered as a plain number is left out, and the property of the same name returns " +
                   "it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            names.Add("rotation_angle_deg", new string[] { "rotation_angle" });
            return ModelItemValueReader.GetScalarEquations(Resolve(), names);
        }
        public override string ToString()
        {
            MaterialOrientation materialOrientation = Resolve();
            return "MaterialOrientation(name=" + StringListAPI.ToDisplayString(materialOrientation.Name) +
                   ", region=" + StringListAPI.ToDisplayString(materialOrientation.RegionName) +
                   ", reference_axis=" + materialOrientation.ReferenceAxis + ")";
        }
        private MaterialOrientation Resolve()
        {
            return _service.ResolveMaterialOrientation(_materialOrientationName);
        }
    }
}
