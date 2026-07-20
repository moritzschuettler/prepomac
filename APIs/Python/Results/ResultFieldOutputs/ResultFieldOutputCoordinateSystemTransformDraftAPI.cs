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
    public class ResultFieldOutputCoordinateSystemTransformDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly string _coordinateSystemName;


        // Properties
        [PythonAPI("The source vector or tensor field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The result coordinate system name.", Returns = "str")]
        public string coordinate_system_name { get { return _coordinateSystemName; } }


        // Constructors
        internal ResultFieldOutputCoordinateSystemTransformDraftAPI(string name, string fieldName,
                                                                    string coordinateSystemName)
            : base(name)
        {
            _fieldName = fieldName;
            _coordinateSystemName = coordinateSystemName;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            return new ResultFieldOutputCoordinateSystemTransform(name, _fieldName, _coordinateSystemName);
        }
        public override string ToString()
        {
            return "FieldOutputCoordinateSystemTransformDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", coordinate_system_name=" + StringListAPI.ToDisplayString(_coordinateSystemName) + ")";
        }
    }
}
