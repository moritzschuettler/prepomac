// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromIntegrationDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly IntegralTypeEnum _integralType;
        private readonly IntegralResultEnum _integralResult;
        private readonly string _fieldName;
        private readonly string[] _componentNames;
        private readonly string _deformationVariableName;
        private readonly IntegrationScaleType _crossSection;
        private readonly double _thickness;
        private string _unit;


        // Properties
        [PythonAPI("The integration type. Possible values are 'line', 'surface' and 'volume'.", Returns = "str")]
        public string integral_type { get { return IntegralTypeToString(_integralType); } }
        [PythonAPI("The integral result. Possible values are 'geometry', 'field' and 'field_by_geometry'.",
                   Returns = "str")]
        public string integral_result { get { return IntegralResultToString(_integralResult); } }
        [PythonAPI("The source result field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The selected field component names.", Returns = "StringList")]
        public StringListAPI component_names { get { return new StringListAPI(_componentNames); } }
        [PythonAPI("The deformation variable name.", Returns = "str")]
        public string deformation_variable_name { get { return _deformationVariableName; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return _unit; } set { _unit = value; } }
        [PythonAPI("The out-of-plane cross section scaling for 2D output. Possible values are 'none', " +
                   "'thickness' and 'axisymmetric'.", Returns = "str")]
        public string cross_section { get { return CrossSectionToString(_crossSection); } }
        [PythonAPI("The out-of-plane thickness used when cross_section is 'thickness'.", Returns = "float")]
        public double thickness { get { return _thickness; } }


        // Constructors
        internal ResultHistoryOutputFromIntegrationDraftAPI(string name, IntegralTypeEnum integralType,
                                                             IntegralResultEnum integralResult,
                                                             string fieldName, string[] componentNames,
                                                             object region,
                                                             string deformationVariableName, string unit,
                                                             IntegrationScaleType crossSection, double thickness)
            : base(name, region)
        {
            _integralType = integralType;
            _integralResult = integralResult;
            _fieldName = fieldName ?? "";
            _componentNames = componentNames ?? new string[0];
            _deformationVariableName = deformationVariableName;
            _unit = unit;
            _crossSection = crossSection;
            _thickness = thickness;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromIntegration output =
                new ResultHistoryOutputFromIntegration(name, _integralType, "", RegionTypeEnum.None,
                                                       _fieldName, _componentNames, _deformationVariableName);
            output.IntegralResult = _integralResult;
            output.Unit = _unit;
            output.CrossSection = _crossSection;
            output.Thickness = _thickness;
            return output;
        }
        protected override string GetRequiredTarget()
        {
            if (_integralType == IntegralTypeEnum.Line) return "edges";
            if (_integralType == IntegralTypeEnum.Surface) return "surfaces";
            return "elements";
        }
        private static string IntegralTypeToString(IntegralTypeEnum integralType)
        {
            if (integralType == IntegralTypeEnum.Line) return "line";
            if (integralType == IntegralTypeEnum.Surface) return "surface";
            return "volume";
        }
        private static string IntegralResultToString(IntegralResultEnum integralResult)
        {
            if (integralResult == IntegralResultEnum.Geometry) return "geometry";
            if (integralResult == IntegralResultEnum.FieldByGeometry) return "field_by_geometry";
            return "field";
        }
        private static string CrossSectionToString(IntegrationScaleType crossSection)
        {
            if (crossSection == IntegrationScaleType.Thickness) return "thickness";
            if (crossSection == IntegrationScaleType.Axisymmetric) return "axisymmetric";
            return "none";
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
