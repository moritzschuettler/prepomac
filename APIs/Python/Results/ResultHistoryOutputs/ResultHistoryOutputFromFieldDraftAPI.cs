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
        [PythonAPI("The complex result representation: Real, Imaginary, Magnitude, Phase, RealAtAngle, Max, " +
                   "AngleAtMax, Min or AngleAtMin.", Returns = "str")]
        public object complex_result_type
        {
            get { return _complexResultType.ToString(); }
            set
            {
                string text = PythonValueConverter.ToText(value, "complex_result_type", false);
                _complexResultType = PythonValueConverter.ToEnum<ComplexResultTypeEnum>(text, "complex_result_type");
            }
        }
        [PythonAPI("The complex result angle in degrees.", Returns = "float")]
        public object complex_angle_deg
        {
            get { return _complexAngleDeg; }
            set { _complexAngleDeg = PythonValueConverter.ToFiniteDouble(value, "complex_angle_deg"); }
        }
        [PythonAPI("The result step ID, or None for all steps.", Returns = "int or None")]
        public object step_id
        {
            get { return _stepId; }
            set { _stepId = value == null ? (int?)null : PythonValueConverter.ToInt(value, "step_id"); }
        }
        [PythonAPI("The result increment ID, or None for all increments.", Returns = "int or None")]
        public object increment_id
        {
            get { return _incrementId; }
            set { _incrementId = value == null ? (int?)null : PythonValueConverter.ToInt(value, "increment_id"); }
        }
        [PythonAPI("Whether harmonic oscillations are included.", Returns = "bool")]
        public object harmonic
        {
            get { return _harmonic; }
            set { _harmonic = PythonValueConverter.ToBool(value, "harmonic"); }
        }
        [PythonAPI("Node coordinate output: Off, Undeformed or Deformed.", Returns = "str")]
        public object output_node_coordinates
        {
            get { return _outputNodeCoordinates.ToString(); }
            set
            {
                string text = PythonValueConverter.ToText(value, "output_node_coordinates", false);
                _outputNodeCoordinates = PythonValueConverter.ToEnum<OutputNodeCoordinatesEnum>(text,
                                                                                                "output_node_coordinates");
            }
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
        public override string ToString()
        {
            return "HistoryOutputFromFieldDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", components=" + _componentNames.Length + ")";
        }
    }
}
