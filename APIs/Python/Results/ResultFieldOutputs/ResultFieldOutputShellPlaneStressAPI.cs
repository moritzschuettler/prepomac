// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultFieldOutputShellPlaneStressAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return ResolveShellPlaneStress().FieldName; } }


        // Constructor
        internal ResultFieldOutputShellPlaneStressAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputShellPlaneStress ResolveShellPlaneStress()
        {
            ResultFieldOutputShellPlaneStress fieldOutput = Resolve() as ResultFieldOutputShellPlaneStress;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName + "' is not a shell plane stress field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputShellPlaneStress fieldOutput = ResolveShellPlaneStress();
            return "FieldOutputShellPlaneStress(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) + ")";
        }
    }
}
