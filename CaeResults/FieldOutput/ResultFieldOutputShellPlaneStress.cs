// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeResults
{
    // The shell in-plane state: the in-plane components and the in-plane principal values, in the material axes of the
    // shell elements, of their stresses or of their strains, neglecting the components through the thickness
    [PmxType(5029)]
    public class ResultFieldOutputShellPlaneStress : ResultFieldOutput
    {
        // Variables                                                                                                                
        public const string ComponentS11 = "S11";
        public const string ComponentS22 = "S22";
        public const string ComponentS12 = "S12";
        public const string ComponentE11 = "E11";
        public const string ComponentE22 = "E22";
        public const string ComponentE12 = "E12";
        public const string ComponentPrincipalMax = "PRINCIPAL_MAX";
        public const string ComponentPrincipalMin = "PRINCIPAL_MIN";
        public const string ComponentShearMax = "SHEAR_MAX";
        public const string ComponentAngle = "ANGLE";
        [PmxKey(0)] private string _fieldName;
        [PmxKey(1)] [OptionalField]
        private ShellInPlaneQuantityEnum _quantity;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public ShellInPlaneQuantityEnum Quantity { get { return _quantity; } set { _quantity = value; } }


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
            if (_quantity == ShellInPlaneQuantityEnum.Strain)
                return new string[] { ComponentE11, ComponentE22, ComponentE12, ComponentPrincipalMax, ComponentPrincipalMin,
                                      ComponentShearMax, ComponentAngle };
            return new string[] { ComponentS11, ComponentS22, ComponentS12, ComponentPrincipalMax, ComponentPrincipalMin,
                                  ComponentShearMax, ComponentAngle };
        }
    }
}
