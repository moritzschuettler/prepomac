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
    public class ResultFieldOutputShellInPlaneStateAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return ResolveShellInPlaneState().FieldName; } }
        [PythonAPI("The in-plane quantity: Stress for the stresses as CalculiX solves them, PlaneStressFromStrain " +
                   "for the plane stresses the elastic constants of the ply material give the in-plane strains, " +
                   "or Strain for the in-plane strains.", Returns = "str")]
        public string quantity { get { return ResolveShellInPlaneState().Quantity.ToString(); } }


        // Constructor
        internal ResultFieldOutputShellInPlaneStateAPI(string resultName, string fieldOutputName,
                                                       ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputShellPlaneStress ResolveShellInPlaneState()
        {
            ResultFieldOutputShellPlaneStress fieldOutput = Resolve() as ResultFieldOutputShellPlaneStress;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName +
                                             "' is not a shell in-plane state field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputShellPlaneStress fieldOutput = ResolveShellInPlaneState();
            return "FieldOutputShellInPlaneState(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) +
                   ", quantity=" + StringListAPI.ToDisplayString(fieldOutput.Quantity.ToString()) + ")";
        }
    }
}
