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
    public class ResultFieldOutputEnvelopeAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source field name.", Returns = "str")]
        public string field_name { get { return ResolveEnvelope().FieldName; } }
        [PythonAPI("The source field component name.", Returns = "str")]
        public string component_name { get { return ResolveEnvelope().ComponentName; } }


        // Constructors
        internal ResultFieldOutputEnvelopeAPI(string resultName, string fieldOutputName,
                                              ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputEnvelope ResolveEnvelope()
        {
            ResultFieldOutputEnvelope fieldOutput = Resolve() as ResultFieldOutputEnvelope;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName +
                                             "' is not an envelope field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputEnvelope fieldOutput = ResolveEnvelope();
            return "FieldOutputEnvelope(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) +
                   ", component_name=" + StringListAPI.ToDisplayString(fieldOutput.ComponentName) + ")";
        }
    }
}
