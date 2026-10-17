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
    // 'damping_type' a different one on damping than on modal damping. A property is keyed as its
    // declaring type, its name and its name again where it reads another enumeration than the
    // argument of the same name.
    //
    // A member that accepts only some values of the enumeration lists only those: its key carries
    // the allowed values beside the enumeration, so the schema never offers a value the member
    // always refuses.
    //
    // Test_Api_Enum_Vocabulary.py checks this against every ToEnum site in the API sources, so an
    // argument added without an entry here is reported rather than silently undescribed.
    public static class PythonEnumVocabulary
    {
        // Variables
        private static Dictionary<string, Type> _types;
        private static Dictionary<string, string[]> _allowedValues;
        private static Dictionary<string, string[]> _lowerCaseNames;


        // Static methods
        // The values the argument accepts, or null when the argument is not enumeration valued
        public static string[] GetValues(string declaringTypeName, string memberName, string argumentName)
        {
            string[] lowerCaseNames;
            if (declaringTypeName != null && memberName != null &&
                GetLowerCaseNames().TryGetValue(declaringTypeName + "." + memberName + "." + argumentName,
                                                out lowerCaseNames))
                return lowerCaseNames;
            //
            string key = GetKey(declaringTypeName, memberName, argumentName);
            if (key == null) return null;
            //
            string[] allowedValues;
            if (GetAllowedValues().TryGetValue(key, out allowedValues)) return allowedValues;
            return Enum.GetNames(GetTypes()[key]);
        }
        // The values the member accepts when it accepts only some of the enumeration, or null when it accepts
        // them all or the argument is not enumeration valued
        public static string[] GetNarrowedValues(string declaringTypeName, string memberName, string argumentName)
        {
            string key = GetKey(declaringTypeName, memberName, argumentName);
            if (key == null) return null;
            //
            string[] allowedValues;
            return GetAllowedValues().TryGetValue(key, out allowedValues) ? allowedValues : null;
        }
        public static Type GetType(string declaringTypeName, string memberName, string argumentName)
        {
            string key = GetKey(declaringTypeName, memberName, argumentName);
            if (key == null) return null;
            //
            return GetTypes()[key];
        }
        private static string GetKey(string declaringTypeName, string memberName, string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName)) return null;
            //
            Dictionary<string, Type> types = GetTypes();
            string key;
            if (declaringTypeName != null && memberName != null)
            {
                key = declaringTypeName + "." + memberName + "." + argumentName;
                if (types.ContainsKey(key)) return key;
            }
            if (memberName != null)
            {
                key = memberName + "." + argumentName;
                if (types.ContainsKey(key)) return key;
            }
            if (types.ContainsKey(argumentName)) return argumentName;
            //
            return null;
        }
        // The keys whose member accepts only some values of the enumeration, with those values
        public static Dictionary<string, string[]> GetAllowedValues()
        {
            if (_allowedValues != null) return _allowedValues;
            //
            string[] referencePointBySelection = Names(FeReferencePointCreatedFrom.OnPoint,
                                                       FeReferencePointCreatedFrom.BetweenTwoPoints,
                                                       FeReferencePointCreatedFrom.CircleCenter);
            string[] referencePointByRegion = Names(FeReferencePointCreatedFrom.CenterOfGravity,
                                                    FeReferencePointCreatedFrom.BoundingBoxCenter);
            string[] coordinateSystemPointBySelection = Names(CsPointCreatedFromEnum.OnPoint,
                                                              CsPointCreatedFromEnum.BetweenTwoPoints,
                                                              CsPointCreatedFromEnum.CircleCenter);
            Dictionary<string, string[]> allowedValues = new Dictionary<string, string[]>(StringComparer.Ordinal);
            // Undefined is the state of a model nothing has set yet, which no member sets
            allowedValues.Add("model_space", NamesExcept(ModelSpaceEnum.Undefined));
            allowedValues.Add("unit_system", NamesExcept(UnitSystemType.Undefined));
            allowedValues.Add("add_from_selection.created_from", referencePointBySelection);
            allowedValues.Add("replace_from_selection.created_from", referencePointBySelection);
            allowedValues.Add("add_from_region.created_from", referencePointByRegion);
            allowedValues.Add("replace_from_region.created_from", referencePointByRegion);
            // The reference point dialog takes a node set or a surface as its region
            string[] referencePointRegionTypes = Names(RegionTypeEnum.NodeSetName, RegionTypeEnum.SurfaceName);
            allowedValues.Add("add_from_region.region_type", referencePointRegionTypes);
            allowedValues.Add("replace_from_region.region_type", referencePointRegionTypes);
            // A region names a set, a surface, a reference point, a part or a mass section of the model
            string[] namedRegionTypes = new string[RegionAPI.NamedRegionTypes.Length];
            for (int i = 0; i < namedRegionTypes.Length; i++)
                namedRegionTypes[i] = RegionAPI.NamedRegionTypes[i].ToString();
            allowedValues.Add("CreateAPI.region.region_type", namedRegionTypes);
            // Every pick, area and box selection refuses the modes that pick nothing, and every operation
            // refuses Invert, which invert() adds
            allowedValues.Add("select_by", NamesExcept(vtkSelectBy.Off, vtkSelectBy.Default, vtkSelectBy.Id,
                                                       vtkSelectBy.Widget));
            allowedValues.Add("operation", NamesExcept(vtkSelectOperation.Invert));
            // The joint centre picked by nodes in the dialog has no argument; a script gives its coordinates
            allowedValues.Add("center_defined_by", Names(JointCenterDefinitionEnum.MasterSurface,
                                                         JointCenterDefinitionEnum.Coordinates));
            foreach (string point in new[] { "origin", "point_on_x", "point_in_xy" })
            {
                allowedValues.Add("add_from_selections." + point + "_created_from", coordinateSystemPointBySelection);
                allowedValues.Add("replace_from_selections." + point + "_created_from",
                                  coordinateSystemPointBySelection);
            }
            //
            _allowedValues = allowedValues;
            return _allowedValues;
        }
        // The arguments that take one of a few lower case names rather than an enumeration name - the view, the
        // target and the source of a selection - keyed by the declaring type, the member and the argument. They
        // are parsed by the selection service, regardless of case, and listed here only for the schema
        public static Dictionary<string, string[]> GetLowerCaseNames()
        {
            if (_lowerCaseNames != null) return _lowerCaseNames;
            //
            Dictionary<string, string[]> names = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (string member in new[] { "selection_by_ids", "selection_by_pick", "selection_by_area",
                                              "selection_by_box", "selection_all" })
            {
                names.Add("CreateAPI." + member + ".view", SelectionService.ViewNames);
                names.Add("CreateAPI." + member + ".target",
                          member == "selection_all" ? SelectionService.AllTargetNames : SelectionService.TargetNames);
            }
            names.Add("CreateAPI.selection_by_ids.source", SelectionService.SourceNames);
            names.Add("SelectionAPI.add_by_ids.source", SelectionService.SourceNames);
            // The displayed tree and the tree a group operation acts on are named by the same three views
            names.Add("ViewAPI.current.current", SelectionService.ViewNames);
            foreach (string member in new[] { "get_item_types", "add_group", "rename_group", "remove_groups",
                                              "move_items", "move_group", "get_layout", "set_layout" })
                names.Add("TreeAPI." + member + ".view", SelectionService.ViewNames);
            //
            _lowerCaseNames = names;
            return _lowerCaseNames;
        }
        private static string[] NamesExcept(params Enum[] excludedValues)
        {
            HashSet<string> excludedNames = new HashSet<string>();
            foreach (Enum excludedValue in excludedValues) excludedNames.Add(excludedValue.ToString());
            List<string> names = new List<string>();
            foreach (string name in Enum.GetNames(excludedValues[0].GetType()))
            {
                if (!excludedNames.Contains(name)) names.Add(name);
            }
            return names.ToArray();
        }
        private static string[] Names(params Enum[] values)
        {
            string[] names = new string[values.Length];
            for (int i = 0; i < values.Length; i++) names[i] = values[i].ToString();
            return names;
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
            types.Add("CreateAPI.region.region_type", typeof(RegionTypeEnum));
            types.Add("add_from_region.region_type", typeof(RegionTypeEnum));
            types.Add("replace_from_region.region_type", typeof(RegionTypeEnum));
            types.Add("select_by", typeof(vtkSelectBy));
            types.Add("operation", typeof(vtkSelectOperation));
            types.Add("geometry_select_mode", typeof(GeometrySelectModeEnum));
            types.Add("created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("add_from_selection.created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("replace_from_selection.created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("add_from_region.created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("replace_from_region.created_from", typeof(FeReferencePointCreatedFrom));
            types.Add("SurfaceAPI.created_from.created_from", typeof(FeSurfaceCreatedFrom));
            types.Add("coordinate_system_type", typeof(CoordinateSystemTypeEnum));
            types.Add("add_from_selections.origin_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("replace_from_selections.origin_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("add_from_selections.point_on_x_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("replace_from_selections.point_on_x_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("add_from_selections.point_in_xy_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("replace_from_selections.point_in_xy_created_from", typeof(CsPointCreatedFromEnum));
            types.Add("limit_to", typeof(SelectionLimitToEnum));
            types.Add("surface_type", typeof(FeSurfaceType));
            // Materials
            types.Add("hardening", typeof(PlasticHardening));
            // Material orientations
            types.Add("reference_axis", typeof(MaterialOrientationAxisEnum));
            // Composite failure
            types.Add("criterion", typeof(CompositeFailureCriterionEnum));
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
            types.Add("ContactPairAPI.method.method", typeof(ContactPairMethod));
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
            types.Add("DistributionAPI.interpolator_type.interpolator_type", typeof(CloudInterpolatorEnum));
            types.Add("based_on", typeof(LimitPlotBasedOnEnum));
            types.Add("ResultFieldOutputsAPI.create_median_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("ResultFieldOutputsAPI.create_average_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("ResultFieldOutputsAPI.create_smooth_filter.source", typeof(FieldResultFilterSourceTypeEnum));
            types.Add("ResultFieldOutputFilterAPI.source.source", typeof(FieldResultFilterSourceTypeEnum));
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
