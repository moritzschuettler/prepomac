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
    public class ResultFieldOutputMaterialAxesAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return ResolveMaterialAxes().FieldName; } }


        // Constructor
        internal ResultFieldOutputMaterialAxesAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputMaterialAxes ResolveMaterialAxes()
        {
            ResultFieldOutputMaterialAxes fieldOutput = Resolve() as ResultFieldOutputMaterialAxes;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName + "' is not a material-axes field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputMaterialAxes fieldOutput = ResolveMaterialAxes();
            return "FieldOutputMaterialAxes(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) + ")";
        }
    }
}
