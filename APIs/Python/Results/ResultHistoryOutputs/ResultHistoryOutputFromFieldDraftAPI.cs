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
    public class ResultHistoryOutputFromFieldDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly string[] _componentNames;
        private ComplexResultTypeEnum _complexResultType;
        private double _complexAngleDeg;
        private int? _stepId;
        private int? _incrementId;
        private bool _harmonic;
        private OutputNodeCoordinatesEnum _outputNodeCoordinates;


        // Properties
        [PythonAPI("The source result field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The selected field component names.", Returns = "StringList")]
        public StringListAPI component_names { get { return new StringListAPI(_componentNames); } }
        [PythonAPI("The complex result representation. Possible values are 'real', 'imaginary', 'magnitude', " +
                   "'phase', 'angle', 'max', 'angle_at_max', 'min', and 'angle_at_min'.", Returns = "str")]
        public string complex_result_type
        {
            get { return FormatComplexResultType(_complexResultType); }
            set { _complexResultType = ParseComplexResultType(value); }
        }
        [PythonAPI("The complex result angle in degrees.", Returns = "float")]
        public double complex_angle_deg { get { return _complexAngleDeg; } set { _complexAngleDeg = value; } }
        [PythonAPI("The result step ID, or None for all steps.", Returns = "int or None")]
        public int? step_id { get { return _stepId; } set { _stepId = value; } }
        [PythonAPI("The result increment ID, or None for all increments.", Returns = "int or None")]
        public int? increment_id { get { return _incrementId; } set { _incrementId = value; } }
        [PythonAPI("Whether harmonic oscillations are included.", Returns = "bool")]
        public bool harmonic { get { return _harmonic; } set { _harmonic = value; } }
        [PythonAPI("Node coordinate output. Possible values are 'off', 'undeformed', and 'deformed'.",
                   Returns = "str")]
        public string output_node_coordinates
        {
            get { return FormatOutputNodeCoordinates(_outputNodeCoordinates); }
            set { _outputNodeCoordinates = ParseOutputNodeCoordinates(value); }
        }


        // Constructors
        internal ResultHistoryOutputFromFieldDraftAPI(string name, string fieldName, string[] componentNames,
                                                       object region)
            : base(name, region)
        {
            _fieldName = fieldName;
            _componentNames = componentNames;
            _complexResultType = ComplexResultTypeEnum.Real;
            _complexAngleDeg = 0;
            _stepId = null;
            _incrementId = null;
            _harmonic = false;
            _outputNodeCoordinates = OutputNodeCoordinatesEnum.Off;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromField output =
                new ResultHistoryOutputFromField(name, "", RegionTypeEnum.None, _fieldName, _componentNames);
            output.ComplexResultType = _complexResultType;
            output.ComplexAngleDeg = _complexAngleDeg;
            output.StepId = _stepId ?? -1;
            output.StepIncrementId = _incrementId ?? -1;
            output.Harmonic = _harmonic;
            output.OutputNodeCoordinates = _outputNodeCoordinates;
            return output;
        }
        protected override string GetRequiredTarget()
        {
            return "nodes";
        }
        private static ComplexResultTypeEnum ParseComplexResultType(string value)
        {
            if (value == "real") return ComplexResultTypeEnum.Real;
            if (value == "imaginary") return ComplexResultTypeEnum.Imaginary;
            if (value == "magnitude") return ComplexResultTypeEnum.Magnitude;
            if (value == "phase") return ComplexResultTypeEnum.Phase;
            if (value == "angle") return ComplexResultTypeEnum.RealAtAngle;
            if (value == "max") return ComplexResultTypeEnum.Max;
            if (value == "angle_at_max") return ComplexResultTypeEnum.AngleAtMax;
            if (value == "min") return ComplexResultTypeEnum.Min;
            if (value == "angle_at_min") return ComplexResultTypeEnum.AngleAtMin;
            throw new PythonAPIException("Unsupported complex_result_type '" + value + "'.");
        }
        internal static string FormatComplexResultType(ComplexResultTypeEnum value)
        {
            if (value == ComplexResultTypeEnum.Real) return "real";
            if (value == ComplexResultTypeEnum.Imaginary) return "imaginary";
            if (value == ComplexResultTypeEnum.Magnitude) return "magnitude";
            if (value == ComplexResultTypeEnum.Phase) return "phase";
            if (value == ComplexResultTypeEnum.RealAtAngle) return "angle";
            if (value == ComplexResultTypeEnum.Max) return "max";
            if (value == ComplexResultTypeEnum.AngleAtMax) return "angle_at_max";
            if (value == ComplexResultTypeEnum.Min) return "min";
            if (value == ComplexResultTypeEnum.AngleAtMin) return "angle_at_min";
            throw new PythonAPIException("Unsupported complex result type '" + value + "'.");
        }
        private static OutputNodeCoordinatesEnum ParseOutputNodeCoordinates(string value)
        {
            if (value == "off") return OutputNodeCoordinatesEnum.Off;
            if (value == "undeformed") return OutputNodeCoordinatesEnum.Undeformed;
            if (value == "deformed") return OutputNodeCoordinatesEnum.Deformed;
            throw new PythonAPIException("The output_node_coordinates value must be 'off', 'undeformed', or " +
                                         "'deformed'.");
        }
        internal static string FormatOutputNodeCoordinates(OutputNodeCoordinatesEnum value)
        {
            if (value == OutputNodeCoordinatesEnum.Off) return "off";
            if (value == OutputNodeCoordinatesEnum.Undeformed) return "undeformed";
            if (value == OutputNodeCoordinatesEnum.Deformed) return "deformed";
            throw new PythonAPIException("Unsupported node coordinate output type '" + value + "'.");
        }
        public override string ToString()
        {
            return "HistoryOutputFromFieldDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", components=" + _componentNames.Length + ")";
        }
    }
}
