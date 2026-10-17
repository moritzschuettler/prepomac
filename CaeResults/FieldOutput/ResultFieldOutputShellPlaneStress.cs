// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeResults
{
    // The in-plane components and the in-plane principal values of a tensor field in the material axes of the shell
    // elements, assuming a plane state that neglects the components through the thickness
    [Serializable]
    public class ResultFieldOutputShellPlaneStress : ResultFieldOutput
    {
        // Variables
        public const string ComponentS11 = "S11";
        public const string ComponentS22 = "S22";
        public const string ComponentS12 = "S12";
        public const string ComponentPrincipalMax = "PRINCIPAL_MAX";
        public const string ComponentPrincipalMin = "PRINCIPAL_MIN";
        public const string ComponentShearMax = "SHEAR_MAX";
        public const string ComponentAngle = "ANGLE";
        private string _fieldName;


        // Properties
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }


        // Constructor
        public ResultFieldOutputShellPlaneStress(string name, string fieldName)
            : base(name)
        {
            _fieldName = fieldName;
        }


        // Methods
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public override string[] GetComponentNames()
        {
            return new string[] { ComponentS11, ComponentS22, ComponentS12, ComponentPrincipalMax, ComponentPrincipalMin,
                                  ComponentShearMax, ComponentAngle };
        }
    }
}
