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
    public class ResultHistoryOutputFromFieldAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The source result field name.", Returns = "str")]
        public string field_name { get { return ResolveFromField().FieldName; } }
        [PythonAPI("The selected field component names.", Returns = "StringList")]
        public StringListAPI component_names { get { return new StringListAPI(ResolveFromField().ComponentNames); } }
        [PythonAPI("The region type: Selection, NodeSetName or SurfaceName.", Returns = "str")]
        public string region_type { get { return ResolveFromField().RegionType.ToString(); } }
        [PythonAPI("The region name, or None when the region is a selection.", Returns = "str or None")]
        public string region_name { get { return FormatRegionName(ResolveFromField()); } }
        [PythonAPI("The complex result representation: Real, Imaginary, Magnitude, Phase, RealAtAngle, Max, " +
                   "AngleAtMax, Min or AngleAtMin.", Returns = "str")]
        public string complex_result_type { get { return ResolveFromField().ComplexResultType.ToString(); } }
        [PythonAPI("The complex result angle in degrees, or None when complex_result_type is not RealAtAngle, " +
                   "the only type that uses it.", Returns = "float or None")]
        public double? complex_angle_deg
        {
            get
            {
                ResultHistoryOutputFromField historyOutput = ResolveFromField();
                if (historyOutput.ComplexResultType != ComplexResultTypeEnum.RealAtAngle) return null;
                return historyOutput.ComplexAngleDeg;
            }
        }
        [PythonAPI("The result step ID, or None when all steps are used.", Returns = "int or None")]
        public int? step_id
        {
            get
            {
                int stepId = ResolveFromField().StepId;
                return stepId == -1 ? (int?)null : stepId;
            }
        }
        [PythonAPI("The result increment ID, or None when all increments are used.", Returns = "int or None")]
        public int? increment_id
        {
            get
            {
                int incrementId = ResolveFromField().StepIncrementId;
                return incrementId == -1 ? (int?)null : incrementId;
            }
        }
        [PythonAPI("Whether harmonic oscillations are included.", Returns = "bool")]
        public bool harmonic { get { return ResolveFromField().Harmonic; } }
        [PythonAPI("Node coordinate output: Off, Undeformed or Deformed.", Returns = "str")]
        public string output_node_coordinates
        {
            get { return ResolveFromField().OutputNodeCoordinates.ToString(); }
        }


        // Constructors
        internal ResultHistoryOutputFromFieldAPI(string resultName, string historyOutputName,
                                                 ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromField ResolveFromField()
        {
            ResultHistoryOutputFromField historyOutput = Resolve() as ResultHistoryOutputFromField;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a From Field history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            ResultHistoryOutputFromField historyOutput = ResolveFromField();
            return "HistoryOutputFromField(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(historyOutput.FieldName) +
                   ", components=" + historyOutput.ComponentNames.Length + ")";
        }
    }
}
