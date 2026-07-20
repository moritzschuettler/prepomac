// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromIntegrationDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly IntegralTypeEnum _integralType;
        private readonly string _fieldName;
        private readonly string[] _componentNames;
        private readonly string _deformationVariableName;
        private string _unit;


        // Properties
        [PythonAPI("The integration type. Possible values are 'surface' and 'volume'.", Returns = "str")]
        public string integral_type { get { return _integralType == IntegralTypeEnum.Surface ? "surface" : "volume"; } }
        [PythonAPI("The source result field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The selected field component names.", Returns = "StringList")]
        public StringListAPI component_names { get { return new StringListAPI(_componentNames); } }
        [PythonAPI("The deformation variable name.", Returns = "str")]
        public string deformation_variable_name { get { return _deformationVariableName; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return _unit; } set { _unit = value; } }


        // Constructors
        internal ResultHistoryOutputFromIntegrationDraftAPI(string name, IntegralTypeEnum integralType,
                                                             string fieldName, string[] componentNames,
                                                             object region,
                                                             string deformationVariableName, string unit)
            : base(name, region)
        {
            _integralType = integralType;
            _fieldName = fieldName;
            _componentNames = componentNames;
            _deformationVariableName = deformationVariableName;
            _unit = unit;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromIntegration output =
                new ResultHistoryOutputFromIntegration(name, _integralType, "", RegionTypeEnum.None,
                                                       _fieldName, _componentNames, _deformationVariableName);
            output.Unit = _unit;
            return output;
        }
        protected override string GetRequiredTarget()
        {
            return _integralType == IntegralTypeEnum.Surface ? "surfaces" : "elements";
        }
        public override string ToString()
        {
            return "HistoryOutputFromIntegrationDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", integral_type=" + StringListAPI.ToDisplayString(integral_type) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", components=" + _componentNames.Length + ")";
        }
    }
}
