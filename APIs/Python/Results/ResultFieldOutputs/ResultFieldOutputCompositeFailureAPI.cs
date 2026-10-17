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
    public class ResultFieldOutputCompositeFailureAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source tensor field name.", Returns = "str")]
        public string field_name { get { return ResolveCompositeFailure().FieldName; } }
        [PythonAPI("The failure criterion: MaxStress, TsaiHill, TsaiWu or Hashin.", Returns = "str")]
        public string criterion { get { return ResolveCompositeFailure().Criterion.ToString(); } }


        // Constructor
        internal ResultFieldOutputCompositeFailureAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputCompositeFailure ResolveCompositeFailure()
        {
            ResultFieldOutputCompositeFailure fieldOutput = Resolve() as ResultFieldOutputCompositeFailure;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName + "' is not a composite failure field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputCompositeFailure fieldOutput = ResolveCompositeFailure();
            return "FieldOutputCompositeFailure(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) +
                   ", criterion=" + StringListAPI.ToDisplayString(fieldOutput.Criterion.ToString()) + ")";
        }
    }
}
