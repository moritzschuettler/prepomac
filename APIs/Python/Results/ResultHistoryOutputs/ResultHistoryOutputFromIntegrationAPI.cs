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
    public class ResultHistoryOutputFromIntegrationAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The integration type: Line, Surface or Volume.", Returns = "str")]
        public string integral_type { get { return ResolveFromIntegration().IntegralType.ToString(); } }
        [PythonAPI("The integral result: Geometry, Field or FieldByGeometry.", Returns = "str")]
        public string integral_result { get { return ResolveFromIntegration().IntegralResult.ToString(); } }
        [PythonAPI("The source result field name.", Returns = "str")]
        public string field_name { get { return ResolveFromIntegration().FieldName; } }
        [PythonAPI("The selected field component names.", Returns = "StringList")]
        public StringListAPI component_names
        {
            get { return new StringListAPI(ResolveFromIntegration().ComponentNames); }
        }
        [PythonAPI("The region type: Selection, ElementSetName or SurfaceName.", Returns = "str")]
        public string region_type { get { return ResolveFromIntegration().RegionType.ToString(); } }
        [PythonAPI("The region name, or None when the region is a selection.", Returns = "str or None")]
        public string region_name { get { return FormatRegionName(ResolveFromIntegration()); } }
        [PythonAPI("The deformation variable name.", Returns = "str")]
        public string deformation_variable_name { get { return ResolveFromIntegration().DeformationVariableName; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return ResolveFromIntegration().Unit; } }
        [PythonAPI("The out-of-plane cross section scaling for 2D output: None, Thickness or Axisymmetric.",
                   Returns = "str")]
        public string cross_section { get { return ResolveFromIntegration().CrossSection.ToString(); } }
        [PythonAPI("The out-of-plane thickness used when cross_section is Thickness.", Returns = "float")]
        public double thickness { get { return ResolveFromIntegration().Thickness; } }


        // Constructors
        internal ResultHistoryOutputFromIntegrationAPI(string resultName, string historyOutputName,
                                                       ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromIntegration ResolveFromIntegration()
        {
            ResultHistoryOutputFromIntegration historyOutput = Resolve() as ResultHistoryOutputFromIntegration;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a From Integration history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            ResultHistoryOutputFromIntegration historyOutput = ResolveFromIntegration();
            return "HistoryOutputFromIntegration(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", integral_type=" + StringListAPI.ToDisplayString(integral_type) +
                   ", integral_result=" + StringListAPI.ToDisplayString(integral_result) +
                   ", field_name=" + StringListAPI.ToDisplayString(historyOutput.FieldName) + ")";
        }
    }
}
