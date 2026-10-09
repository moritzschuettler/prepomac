// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;
using CaeResults;
using FileInOut.Output;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // Which enumeration each string valued argument of the API is validated against. An API member
    // takes these as plain strings, because that is what a Python caller writes, so the enumeration
    // is known only at the ToEnum call inside - nothing reflecting over the member can recover it.
    // This is that mapping, written down once, so that the schema an agent reads can list the values
    // an argument accepts instead of leaving it to be discovered from a rejection.
    //
    // A key is the argument name, optionally qualified by the member and by the declaring type, and
    // the most specific one wins. The qualified forms exist for the few names two areas share:
    // 'variables' means a different vocabulary on a field output than on a history output, and
    // 'damping_type' a different one on damping than on modal damping.
    //
    // Test_Api_Enum_Vocabulary.py checks this against every ToEnum site in the API sources, so an
    // argument added without an entry here is reported rather than silently undescribed.
    public static class PythonEnumVocabulary
    {
        // Variables
        private static Dictionary<string, Type> _types;


        // Static methods
        // The values the argument accepts, or null when the argument is not enumeration valued
        public static string[] GetValues(string declaringTypeName, string memberName, string argumentName)
        {
            Type type = GetType(declaringTypeName, memberName, argumentName);
            if (type == null) return null;
            //
            return Enum.GetNames(type);
        }
        public static Type GetType(string declaringTypeName, string memberName, string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName)) return null;
            //
            Dictionary<string, Type> types = GetTypes();
            Type type;
            if (declaringTypeName != null && memberName != null &&
                types.TryGetValue(declaringTypeName + "." + memberName + "." + argumentName, out type)) return type;
            if (memberName != null && types.TryGetValue(memberName + "." + argumentName, out type)) return type;
            if (types.TryGetValue(argumentName, out type)) return type;
            //
            return null;
        }
        public static Dictionary<string, Type> GetTypes()
        {
            if (_types != null) return _types;
            //
            Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.Ordinal);
            // Model and units
            types.Add("model_space", typeof(ModelSpaceEnum));
            types.Add("model_type", typeof(ModelType));
            types.Add("unit_system", typeof(UnitSystemType));
            types.Add("slip_wear_results", typeof(SlipWearResultsEnum));
            // Exports
            types.Add("export_geometry.format", typeof(GeometryExportFormatEnum));
            types.Add("export_mesh.format", typeof(MeshExportFormatEnum));
            types.Add("export_model.format", typeof(ModelExportFormatEnum));
            types.Add("export_results.format", typeof(ResultsExportFormatEnum));
            // Selections and regions
            types.Add("region_type", typeof(RegionTypeEnum));
            types.Add("select_by", typeof(vtkSelectBy));
            types.Add("operation", typeof(vtkSelectOperation));
            types.Add("geometry_select_mode", typeof(GeometrySelectModeEnum));
            types.Add("created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("coordinate_system_type", typeof(CoordinateSystemTypeEnum));
            types.Add("origin_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("point_on_x_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("point_in_xy_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("limit_to", typeof(SelectionLimitToEnum));
            types.Add("surface_type", typeof(FeSurfaceType));
            // Materials
            types.Add("hardening", typeof(PlasticHardening));
            // Steps, loads and interactions
            types.Add("incrementation_type", typeof(IncrementationTypeEnum));
            types.Add("solver_type", typeof(SolverTypeEnum));
            types.Add("solution_procedure", typeof(SolutionProcedureEnum));
            types.Add("time_span", typeof(AmplitudeTimeSpanEnum));
            types.Add("cutoff", typeof(HydrostaticPressureCutoffEnum));
            types.Add("tension_type", typeof(PreTensionLoadType));
            types.Add("free_dofs", typeof(APIs.Python.Model.DegreeOfFreedomEnum));
            types.Add("fixed_dofs", typeof(APIs.Python.Model.FixedDegreeOfFreedomEnum));
            // Qualified, because the exploded view takes an argument of the same name with other values
            types.Add("ContactPairsAPI.add.method", typeof(ContactPairMethod));
            types.Add("ContactPairsAPI.replace.method", typeof(ContactPairMethod));
            types.Add("joint_type", typeof(JointTypeEnum));
            types.Add("center_defined_by", typeof(JointCenterDefinitionEnum));
            types.Add("axis_defined_by", typeof(JointDefinitionEnum));
            types.Add("formulation", typeof(JointFormulationEnum));
            types.Add("stiffness_defined_by", typeof(JointStiffnessDefinitionEnum));
            types.Add("pressure_overclosure_type", typeof(PressureOverclosureEnum));
            types.Add("damping.damping_type", typeof(DampingTypeEnum));
            types.Add("modal_damping.damping_type", typeof(ModalDampingTypeEnum));
            // Outputs
            types.Add("output", typeof(ElementFieldOutputOutputEnum));
            types.Add("totals_type", typeof(TotalsTypeEnum));
            types.Add("FieldOutputsAPI.add_nodal.variables", typeof(NodalFieldVariable));
            types.Add("FieldOutputsAPI.replace_nodal.variables", typeof(NodalFieldVariable));
            types.Add("FieldOutputsAPI.add_element.variables", typeof(ElementFieldVariable));
            types.Add("FieldOutputsAPI.replace_element.variables", typeof(ElementFieldVariable));
            types.Add("FieldOutputsAPI.add_contact.variables", typeof(ContactFieldVariable));
            types.Add("FieldOutputsAPI.replace_contact.variables", typeof(ContactFieldVariable));
            types.Add("HistoryOutputsAPI.add_nodal.variables", typeof(NodalHistoryVariable));
            types.Add("HistoryOutputsAPI.replace_nodal.variables", typeof(NodalHistoryVariable));
            types.Add("HistoryOutputsAPI.add_element.variables", typeof(ElementHistoryVariable));
            types.Add("HistoryOutputsAPI.replace_element.variables", typeof(ElementHistoryVariable));
            types.Add("HistoryOutputsAPI.add_contact.variables", typeof(ContactHistoryVariable));
            types.Add("HistoryOutputsAPI.replace_contact.variables", typeof(ContactHistoryVariable));
            types.Add("HistoryOutputsAPI.add_section.variables", typeof(SectionHistoryVariableEnum));
            types.Add("HistoryOutputsAPI.replace_section.variables", typeof(SectionHistoryVariableEnum));
            // Meshing
            types.Add("algorithm_2d", typeof(GmshAlgorithmMesh2DEnum));
            types.Add("algorithm_3d", typeof(GmshAlgorithmMesh3DEnum));
            types.Add("algorithm_recombine", typeof(GmshAlgorithmRecombineEnum));
            types.Add("optimize_high_order", typeof(GmshOptimizeHighOrderEnum));
            types.Add("optimize_first_order_shell", typeof(GmshOptimizeFirstOrderShellEnum));
            types.Add("optimize_first_order_solid", typeof(GmshOptimizeFirstOrderSolidEnum));
            types.Add("triangulation_mesher", typeof(QuadWildTriangulationMesherEnum));
            types.Add("flow_profile", typeof(QuadWildFlowProfileEnum));
            types.Add("boundary_layer", typeof(QuadWildBoundaryLayerEnum));
            types.Add("add_sweep_mesh.algorithm_2d", typeof(SweepAlgorithmMesh2DEnum));
            types.Add("replace_sweep_mesh.algorithm_2d", typeof(SweepAlgorithmMesh2DEnum));
            types.Add("quad_wild_flow_profile", typeof(QuadWildFlowProfileEnum));
            types.Add("symmetry", typeof(SweepSymmetryEnum));
            types.Add("transfinite_three_sided", typeof(TransfiniteThreeSidedEnum));
            types.Add("nodes_to_keep", typeof(NodesToKeepEnum));
            types.Add("metric", typeof(GmshElementQualityMetricEnum));
            // Analyses and results
            types.Add("fem_solver", typeof(FEMSolverEnum));
            types.Add("interpolator_type", typeof(InterpolatorEnum));
            types.Add("add_from_file.interpolator_type", typeof(CloudInterpolatorEnum));
            types.Add("replace_from_file.interpolator_type", typeof(CloudInterpolatorEnum));
            types.Add("based_on", typeof(LimitPlotBasedOnEnum));
            types.Add("ResultFieldOutputsAPI.create_median_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("ResultFieldOutputsAPI.create_average_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("ResultFieldOutputsAPI.create_smooth_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("integral_type", typeof(IntegralTypeEnum));
            types.Add("integral_result", typeof(IntegralResultEnum));
            types.Add("cross_section", typeof(IntegrationScaleType));
            types.Add("complex_result_type", typeof(ComplexResultTypeEnum));
            types.Add("output_node_coordinates", typeof(OutputNodeCoordinatesEnum));
            types.Add("deformation_variable_name", typeof(DeformationVariableEnum));
            types.Add("return_value", typeof(HistoryFilterReturnValueEnum));
            types.Add("operate_on", typeof(HistoryFilterOperateOnEnum));
            // View
            types.Add("deformation_type", typeof(DeformationScaleFactorTypeEnum));
            types.Add("undeformed_model", typeof(UndeformedModelTypeEnum));
            types.Add("result_type", typeof(ViewResultTypeEnum));
            types.Add("orientation", typeof(ViewOrientationEnum));
            types.Add("ViewAPI.apply_exploded_view.method", typeof(ViewExplodedMethodEnum));
            types.Add("axes", typeof(ViewExplodedAxesEnum));
            //
            _types = types;
            return _types;
        }
    }
}
