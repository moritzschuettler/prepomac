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
    public class ResultFieldOutputMaterialAxesDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;


        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }


        // Constructor
        internal ResultFieldOutputMaterialAxesDraftAPI(string name, string fieldName)
            : base(name)
        {
            _fieldName = fieldName;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            return new ResultFieldOutputMaterialAxes(name, _fieldName);
        }
        public override string ToString()
        {
            return "FieldOutputMaterialAxesDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) + ")";
        }
    }
}
