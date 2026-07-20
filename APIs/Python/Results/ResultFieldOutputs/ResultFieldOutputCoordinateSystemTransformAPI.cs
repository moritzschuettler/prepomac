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
    public class ResultFieldOutputCoordinateSystemTransformAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source vector or tensor field name.", Returns = "str")]
        public string field_name { get { return ResolveTransform().FieldName; } }
        [PythonAPI("The result coordinate system name.", Returns = "str")]
        public string coordinate_system_name { get { return ResolveTransform().CoordinateSystemName; } }


        // Constructors
        internal ResultFieldOutputCoordinateSystemTransformAPI(string resultName, string fieldOutputName,
                                                               ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputCoordinateSystemTransform ResolveTransform()
        {
            ResultFieldOutputCoordinateSystemTransform fieldOutput =
                Resolve() as ResultFieldOutputCoordinateSystemTransform;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName +
                                             "' is not a coordinate-system transform field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputCoordinateSystemTransform fieldOutput = ResolveTransform();
            return "FieldOutputCoordinateSystemTransform(name=" +
                   StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) +
                   ", coordinate_system_name=" +
                   StringListAPI.ToDisplayString(fieldOutput.CoordinateSystemName) + ")";
        }
    }
}
