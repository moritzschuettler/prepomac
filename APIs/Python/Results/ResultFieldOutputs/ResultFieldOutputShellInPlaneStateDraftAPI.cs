// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultFieldOutputShellInPlaneStateDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly ShellInPlaneQuantityEnum _quantity;


        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The in-plane quantity: Stress for the stresses as CalculiX solves them, PlaneStressFromStrain " +
                   "for the plane stresses the elastic constants of the ply material give the in-plane strains, " +
                   "or Strain for the in-plane strains.", Returns = "str")]
        public string quantity { get { return _quantity.ToString(); } }


        // Constructor
        internal ResultFieldOutputShellInPlaneStateDraftAPI(string name, string fieldName, ShellInPlaneQuantityEnum quantity)
            : base(name)
        {
            _fieldName = fieldName;
            _quantity = quantity;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            ResultFieldOutputShellPlaneStress fieldOutput = new ResultFieldOutputShellPlaneStress(name, _fieldName);
            fieldOutput.Quantity = _quantity;
            return fieldOutput;
        }
        public override string ToString()
        {
            return "FieldOutputShellInPlaneStateDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", quantity=" + StringListAPI.ToDisplayString(_quantity.ToString()) + ")";
        }
    }
}
