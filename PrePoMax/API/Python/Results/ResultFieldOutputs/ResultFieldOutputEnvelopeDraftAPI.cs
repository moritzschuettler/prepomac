// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    public class ResultFieldOutputEnvelopeDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly string _componentName;


        // Properties
        [PythonAPI("The source field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The source field component name.", Returns = "str")]
        public string component_name { get { return _componentName; } }


        // Constructors
        internal ResultFieldOutputEnvelopeDraftAPI(string name, string fieldName, string componentName)
            : base(name)
        {
            _fieldName = fieldName;
            _componentName = componentName;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            return new ResultFieldOutputEnvelope(name, _fieldName, _componentName);
        }
        public override string ToString()
        {
            return "FieldOutputEnvelopeDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", component_name=" + StringListAPI.ToDisplayString(_componentName) + ")";
        }
    }
}
