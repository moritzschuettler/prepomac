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
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeResults
{
    [PmxType(5020)]
    public enum CompositeFailureCriterionEnum
    {
        [StandardValue("MaxStress", DisplayName = "Maximum stress")]
        MaxStress,
        [StandardValue("TsaiHill", DisplayName = "Tsai-Hill")]
        TsaiHill,
        [StandardValue("TsaiWu", DisplayName = "Tsai-Wu")]
        TsaiWu,
        [StandardValue("Hashin", DisplayName = "Hashin")]
        Hashin
    }

    // A failure criterion of the plies of composite shells, evaluated from the in-plane stresses in the ply axes with
    // the ply strengths of the material of the ply
    [PmxType(5021)]
    public class ResultFieldOutputCompositeFailure : ResultFieldOutput
    {
        // Variables
        public const string ComponentFailureIndex = "FAILURE_INDEX";
        public const string ComponentReserveFactor = "RESERVE_FACTOR";
        public const string ComponentMode = "MODE";
        public const string ComponentMaxFailureIndex = "MAX_FAILURE_INDEX";
        public const string ComponentCriticalLayer = "CRITICAL_LAYER";
        [PmxKey(0)] private string _fieldName;
        [PmxKey(1)] private CompositeFailureCriterionEnum _criterion;
        [PmxKey(2)] [OptionalField]
        private ShellInPlaneQuantityEnum _quantity;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public CompositeFailureCriterionEnum Criterion { get { return _criterion; } set { _criterion = value; } }
        // A failure criterion evaluates stresses, so the in-plane strains are none of its quantities
        public ShellInPlaneQuantityEnum Quantity { get { return _quantity; } set { _quantity = value; } }


        // Constructor
        public ResultFieldOutputCompositeFailure(string name, string fieldName, CompositeFailureCriterionEnum criterion)
            : base(name)
        {
            _fieldName = fieldName;
            _criterion = criterion;
        }


        // Methods
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public override string[] GetComponentNames()
        {
            return new string[] { ComponentFailureIndex, ComponentReserveFactor, ComponentMode, ComponentMaxFailureIndex,
                                  ComponentCriticalLayer };
        }
    }
}
