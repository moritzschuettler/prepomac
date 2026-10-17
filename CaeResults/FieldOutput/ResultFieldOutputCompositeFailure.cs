// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;

namespace CaeResults
{
    [Serializable]
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
    [Serializable]
    public class ResultFieldOutputCompositeFailure : ResultFieldOutput
    {
        // Variables
        public const string ComponentFailureIndex = "FAILURE_INDEX";
        public const string ComponentReserveFactor = "RESERVE_FACTOR";
        public const string ComponentMode = "MODE";
        private string _fieldName;
        private CompositeFailureCriterionEnum _criterion;


        // Properties
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public CompositeFailureCriterionEnum Criterion { get { return _criterion; } set { _criterion = value; } }


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
            return new string[] { ComponentFailureIndex, ComponentReserveFactor, ComponentMode };
        }
    }
}
