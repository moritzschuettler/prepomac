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
    public class ResultFieldOutputCompositeFailureDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly CompositeFailureCriterionEnum _criterion;


        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The failure criterion: MaxStress, TsaiHill, TsaiWu or Hashin.", Returns = "str")]
        public string criterion { get { return _criterion.ToString(); } }


        // Constructor
        internal ResultFieldOutputCompositeFailureDraftAPI(string name, string fieldName, CompositeFailureCriterionEnum criterion)
            : base(name)
        {
            _fieldName = fieldName;
            _criterion = criterion;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            return new ResultFieldOutputCompositeFailure(name, _fieldName, _criterion);
        }
        public override string ToString()
        {
            return "FieldOutputCompositeFailureDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", criterion=" + StringListAPI.ToDisplayString(_criterion.ToString()) + ")";
        }
    }
}
