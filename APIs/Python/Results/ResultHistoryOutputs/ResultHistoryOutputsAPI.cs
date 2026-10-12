// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;
using System.Linq;

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultHistoryOutputsAPI : BaseAPI, IReadOnlyList<ResultHistoryOutputAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly ResultHistoryOutputDataService _dataService;
        private readonly ResultHistoryOutputService _service;


        // Constructors
        internal ResultHistoryOutputsAPI(string resultName, ResultHistoryOutputDataService dataService,
                                         ResultHistoryOutputService service)
        {
            _resultName = resultName;
            _dataService = dataService;
            _service = service;
        }


        // Methods
        public ResultHistoryOutputAPI __getitem__(object historyOutputName)
        {
            string historyOutputNameText = PythonValueConverter.ToText(historyOutputName, "name", false);
            return this[historyOutputNameText];
        }
        public ResultHistoryOutputAPI this[string historyOutputName]
        {
            get
            {
                string storedName = _dataService.ResolveHistoryOutputName(_resultName, historyOutputName);
                return _dataService.CreateAPI(_resultName, storedName);
            }
        }
        public int __len__()
        {
            return _dataService.GetHistoryOutputNames(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultHistoryOutputAPI IReadOnlyList<ResultHistoryOutputAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().ToArray();
                int position = index < 0 ? keys.Length + index : index;
                if (position < 0 || position >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[position]];
            }
        }
        public IEnumerator<ResultHistoryOutputAPI> GetEnumerator()
        {
            foreach (string key in get_names().ToArray()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(object historyOutputName)
        {
            return contains(historyOutputName);
        }

        [PythonAPI("Returns history output names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_dataService.GetHistoryOutputNames(_resultName));
        }

        [PythonAPI("Returns the first history output.", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI first()
        {
            string[] names = _dataService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last history output.", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI last()
        {
            string[] names = _dataService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history output exists.", Arguments = "name: str", Returns = "bool")]
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return _dataService.ContainsHistoryOutput(_resultName, nameText);
        }

        [PythonAPI("Creates a From Field result history-output draft. The region can be a result node selection, " +
                   "a NodeSetName or SurfaceName region created by pmx.create.region and resolved against the " +
                   "result mesh, or a result node set or surface. field_name and component_names are matched " +
                   "regardless of case and stored as the result spells them when the draft is added. This does " +
                   "not modify the result.",
                   Arguments = "name: str, field_name: str, component_names: list[str], " +
                               "region: Selection or Region or NodeSet or Surface",
                   Returns = "HistoryOutputFromFieldDraft")]
        public ResultHistoryOutputFromFieldDraftAPI create_history_output_from_field(
            object name, object field_name, object component_names, object region)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            return _service.CreateFromField(nameText, fieldName, component_names, region);
        }

        [PythonAPI("Creates a From History Output by Equation draft. This does not modify the result.",
                   Arguments = "name: str, equation: str, unit: str = '/'",
                   Returns = "HistoryOutputFromEquationDraft")]
        public ResultHistoryOutputFromEquationDraftAPI create_history_output_from_equation(
            object name, object equation, object unit = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            return _service.CreateFromEquation(nameText, equationText, unitText);
        }

        [PythonAPI("Creates a From Integration draft. Possible integral_type values are Line, Surface and " +
                   "Volume. Line integration accepts an edges-target selection; surface integration accepts a " +
                   "surface-target selection, a SurfaceName region or a result surface; volume integration accepts " +
                   "an element-target selection, an ElementSetName region or a result element set, where a " +
                   "region created by pmx.create.region is resolved against the result mesh. Possible " +
                   "integral_result values are Geometry (the region length/area/volume), Field (the field " +
                   "integral) and FieldByGeometry (the region average of the field); field_name and " +
                   "component_names are required for Field and FieldByGeometry and refused for Geometry, and are " +
                   "matched regardless of case and stored as the result spells them. The " +
                   "cross_section scaling (None, Thickness or Axisymmetric) is the out-of-plane scaling of 2D " +
                   "output; the Line and Volume integrals take it on a 2D and a 3D result alike, as the " +
                   "dialog does, except that adding an Axisymmetric one to a result with a 3D mesh is refused: an " +
                   "axisymmetric model written with 3D output is the 2 degree segment CalculiX computes, and is " +
                   "integrated without a cross section. A Volume integral over 3D elements refuses any value but " +
                   "None. When cross_section is not given it is Axisymmetric for a Line and a Volume integral of " +
                   "an axisymmetric result with a 2D mesh, which so gives the values of the full revolution as " +
                   "its other results do, and None otherwise; 'None' given explicitly keeps the plain 2D measure. " +
                   "The Surface integral refuses any value but None, and integrates the faces of solid and shell " +
                   "elements only: a surface of a 2D result, made of element edges, is refused, and its edges are " +
                   "integrated with the Line integral. The thickness argument is taken only with the " +
                   "Thickness cross_section, where it defaults to 1.0. Possible " +
                   "deformation_variable_name values are Displacements, Forces, SurfaceNormals, WearDepths, " +
                   "MeshDeformation, DispDef and DispDefDepth.",
                   Arguments = "name: str, integral_type: str, " +
                               "region: Selection or Region or ElementSet or Surface, " +
                               "field_name: str = None, component_names: list[str] = None, " +
                               "integral_result: str = 'Geometry', " +
                               "deformation_variable_name: str = 'Displacements', " +
                               "unit: str = '/', cross_section: str = None, thickness: float = None",
                   Returns = "HistoryOutputFromIntegrationDraft")]
        public ResultHistoryOutputFromIntegrationDraftAPI create_history_output_from_integration(
            object name, object integral_type, object region, object field_name = null,
            object component_names = null, object integral_result = null,
            object deformation_variable_name = null, object unit = null,
            object cross_section = null, object thickness = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string integralType = PythonValueConverter.ToText(integral_type, "integral_type", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", true);
            string integralResult =
                PythonValueConverter.ToTextOrDefault(integral_result, "Geometry", "integral_result");
            string deformationVariableName =
                PythonValueConverter.ToTextOrDefault(deformation_variable_name, "Displacements",
                                                     "deformation_variable_name");
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            string crossSection = PythonValueConverter.ToText(cross_section, "cross_section", true);
            return _service.CreateFromIntegration(nameText, integralType, fieldName, component_names, region,
                                                  integralResult, deformationVariableName, unitText,
                                                  crossSection, thickness);
        }

        [PythonAPI("Creates a From Merged Histories draft that merges two to four history components into a " +
                   "single history output. Each source is a HistoryComponent or a list of three strings " +
                   "[history_output_name, field_name, component_name]. All merged entries must share the same unit " +
                   "and a common reference time. This does not modify the result.",
                   Arguments = "name: str, source1: HistoryComponent or list[str], " +
                               "source2: HistoryComponent or list[str], " +
                               "source3: HistoryComponent or list[str] = None, " +
                               "source4: HistoryComponent or list[str] = None",
                   Returns = "HistoryOutputFromMergedHistoriesDraft")]
        public ResultHistoryOutputFromMergedHistoriesDraftAPI create_history_output_from_merged_histories(
            object name, object source1, object source2, object source3 = null, object source4 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            return _service.CreateFromMergedHistories(nameText, source1, source2, source3, source4);
        }

        [PythonAPI("Creates a From Extracted Entry draft that extracts one named entry of a history component into " +
                   "a separate history output. The source is a HistoryComponent or a list of three strings " +
                   "[history_output_name, field_name, component_name]. This does not modify the result.",
                   Arguments = "name: str, source: HistoryComponent or list[str], entry_name: str",
                   Returns = "HistoryOutputFromExtractedEntryDraft")]
        public ResultHistoryOutputFromExtractedEntryDraftAPI create_history_output_from_extracted_entry(
            object name, object source, object entry_name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string entryName = PythonValueConverter.ToText(entry_name, "entry_name", false);
            return _service.CreateFromExtractedEntry(nameText, source, entryName);
        }

        [PythonAPI("Creates an XY draft that plots the entries of the Y history component against the values of " +
                   "the X history component. Each source is a HistoryComponent or a list of three strings " +
                   "[history_output_name, field_name, component_name]. The X component must contain exactly one " +
                   "entry, and the sources must share a common reference time. This does not modify the result.",
                   Arguments = "name: str, x_source: HistoryComponent or list[str], " +
                               "y_source: HistoryComponent or list[str]",
                   Returns = "HistoryOutputXYDraft")]
        public ResultHistoryOutputXYDraftAPI create_history_output_xy(object name, object x_source, object y_source)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            return _service.CreateXY(nameText, x_source, y_source);
        }

        [PythonAPI("Creates a From Table draft from user-defined data. Every row of data_points holds the X value " +
                   "followed by one value per Y column, and all rows must be of equal length. The number of Y " +
                   "columns is taken from the row length; y_column_names defaults to 'Y1', 'Y2', ... " +
                   "This does not modify the result.",
                   Arguments = "name: str, data_points: list[list[float]], y_column_names: list[str] = None, " +
                               "x_axis_unit: str = '/', y_axis_unit: str = '/'",
                   Returns = "HistoryOutputFromTableDraft")]
        public ResultHistoryOutputFromTableDraftAPI create_history_output_from_table(
            object name, object data_points, object y_column_names = null, object x_axis_unit = null,
            object y_axis_unit = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string xAxisUnit = PythonValueConverter.ToTextOrDefault(x_axis_unit, "/", "x_axis_unit");
            string yAxisUnit = PythonValueConverter.ToTextOrDefault(y_axis_unit, "/", "y_axis_unit");
            return _service.CreateFromTable(nameText, data_points, y_column_names, xAxisUnit, yAxisUnit);
        }

        [PythonAPI("Creates a minimum history-output filter. Possible return_value values are Row and Column.",
                   Arguments = "return_value: str = 'Column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_minimum_filter(object return_value = null)
        {
            string returnValue = PythonValueConverter.ToTextOrDefault(return_value, "Column", "return_value");
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Minimum, returnValue);
        }

        [PythonAPI("Creates a maximum history-output filter. Possible return_value values are Row and Column.",
                   Arguments = "return_value: str = 'Column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_maximum_filter(object return_value = null)
        {
            string returnValue = PythonValueConverter.ToTextOrDefault(return_value, "Column", "return_value");
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Maximum, returnValue);
        }

        [PythonAPI("Creates a sum history-output filter. Possible operate_on values are Rows and Columns.",
                   Arguments = "operate_on: str = 'Columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_sum_filter(object operate_on = null)
        {
            string operateOn = PythonValueConverter.ToTextOrDefault(operate_on, "Columns", "operate_on");
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Sum, operateOn);
        }

        [PythonAPI("Creates an average history-output filter. Possible operate_on values are Rows and Columns.",
                   Arguments = "operate_on: str = 'Columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_average_filter(object operate_on = null)
        {
            string operateOn = PythonValueConverter.ToTextOrDefault(operate_on, "Columns", "operate_on");
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Average, operateOn);
        }

        [PythonAPI("Creates a From Field history output and adds it to the current result, in one call. " +
                   "It takes the arguments of create_history_output_from_field, the options of its draft and " +
                   "the optional filters. An option that is not given keeps the default of the draft. As in the " +
                   "dialog, complex_result_type other than Real needs complex results, complex_angle_deg is " +
                   "taken only with RealAtAngle, and harmonic=True only for complex results with Real and a " +
                   "single step_id and increment_id.",
                   Arguments = "name: str, field_name: str, component_names: list[str], " +
                               "region: Selection or Region or NodeSet or Surface, complex_result_type: str = None, " +
                               "complex_angle_deg: float = None, step_id: int = None, increment_id: int = None, " +
                               "harmonic: bool = None, output_node_coordinates: str = None, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_field(object name, object field_name, object component_names,
                                                     object region, object complex_result_type = null,
                                                     object complex_angle_deg = null, object step_id = null,
                                                     object increment_id = null, object harmonic = null,
                                                     object output_node_coordinates = null,
                                                     object filter1 = null,
                                                     object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string complexResultType = PythonValueConverter.ToText(complex_result_type, "complex_result_type", true);
            string outputNodeCoordinates =
                PythonValueConverter.ToText(output_node_coordinates, "output_node_coordinates", true);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            ResultHistoryOutputFromFieldDraftAPI draft = _service.CreateFromField(nameText, fieldName, component_names,
                                                                                  region);
            ApplyFromFieldOptions(draft, complexResultType, complex_angle_deg, step_id, increment_id, harmonic,
                                  outputNodeCoordinates);
            return add(WithFilters(draft, filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Field history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_field",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_field(object old_name, object name, object field_name,
                                                         object component_names, object region,
                                                         object complex_result_type = null,
                                                         object complex_angle_deg = null, object step_id = null,
                                                         object increment_id = null, object harmonic = null,
                                                         object output_node_coordinates = null,
                                                         object filter1 = null,
                                                         object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string complexResultType = PythonValueConverter.ToText(complex_result_type, "complex_result_type", true);
            string outputNodeCoordinates =
                PythonValueConverter.ToText(output_node_coordinates, "output_node_coordinates", true);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            ResultHistoryOutputFromFieldDraftAPI draft = _service.CreateFromField(nameText, fieldName, component_names,
                                                                                  region);
            ApplyFromFieldOptions(draft, complexResultType, complex_angle_deg, step_id, increment_id, harmonic,
                                  outputNodeCoordinates);
            return replace(oldName, WithFilters(draft, filter1Value, filter2Value));
        }

        [PythonAPI("Creates a From Equation history output and adds it to the current result, in one call.",
                   Arguments = "name: str, equation: str, unit: str = '/', " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_equation(object name, object equation, object unit = null,
                                                        object filter1 = null,
                                                        object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFromEquation(nameText, equationText, unitText),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Equation history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_equation",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_equation(object old_name, object name, object equation,
                                                            object unit = null,
                                                            object filter1 = null,
                                                            object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFromEquation(nameText, equationText, unitText),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates a From Integration history output and adds it to the current result, in one " +
                   "call. It takes the arguments of create_history_output_from_integration plus the " +
                   "optional filters.",
                   Arguments = "name: str, integral_type: str, " +
                               "region: Selection or Region or ElementSet or Surface, " +
                               "field_name: str = None, component_names: list[str] = None, " +
                               "integral_result: str = 'Geometry', " +
                               "deformation_variable_name: str = 'Displacements', " +
                               "unit: str = '/', cross_section: str = None, thickness: float = None, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_integration(
            object name, object integral_type, object region, object field_name = null,
            object component_names = null, object integral_result = null,
            object deformation_variable_name = null, object unit = null,
            object cross_section = null, object thickness = null,
            object filter1 = null, object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string integralType = PythonValueConverter.ToText(integral_type, "integral_type", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", true);
            string integralResult =
                PythonValueConverter.ToTextOrDefault(integral_result, "Geometry", "integral_result");
            string deformationVariableName =
                PythonValueConverter.ToTextOrDefault(deformation_variable_name, "Displacements",
                                                     "deformation_variable_name");
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            string crossSection = PythonValueConverter.ToText(cross_section, "cross_section", true);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFromIntegration(nameText, integralType, fieldName,
                                                                   component_names, region, integralResult,
                                                                   deformationVariableName, unitText,
                                                                   crossSection, thickness),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Integration history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_integration",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_integration(
            object old_name, object name, object integral_type, object region, object field_name = null,
            object component_names = null, object integral_result = null,
            object deformation_variable_name = null, object unit = null,
            object cross_section = null, object thickness = null,
            object filter1 = null, object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string integralType = PythonValueConverter.ToText(integral_type, "integral_type", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", true);
            string integralResult =
                PythonValueConverter.ToTextOrDefault(integral_result, "Geometry", "integral_result");
            string deformationVariableName =
                PythonValueConverter.ToTextOrDefault(deformation_variable_name, "Displacements",
                                                     "deformation_variable_name");
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            string crossSection = PythonValueConverter.ToText(cross_section, "cross_section", true);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFromIntegration(nameText, integralType, fieldName,
                                                                                component_names, region,
                                                                                integralResult,
                                                                                deformationVariableName,
                                                                                unitText, crossSection, thickness),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates a From Merged Histories history output and adds it to the current result, in " +
                   "one call.",
                   Arguments = "name: str, source1: HistoryComponent or list[str], " +
                               "source2: HistoryComponent or list[str], " +
                               "source3: HistoryComponent or list[str] = None, " +
                               "source4: HistoryComponent or list[str] = None, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_merged_histories(object name, object source1, object source2,
                                                                object source3 = null, object source4 = null,
                                                                object filter1 = null,
                                                                object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFromMergedHistories(nameText, source1, source2, source3, source4),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Merged Histories history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_merged_histories",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_merged_histories(
            object old_name, object name, object source1, object source2, object source3 = null,
            object source4 = null, object filter1 = null,
            object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFromMergedHistories(nameText, source1, source2,
                                                                                    source3, source4),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates a From Extracted Entry history output and adds it to the current result, in " +
                   "one call.",
                   Arguments = "name: str, source: HistoryComponent or list[str], entry_name: str, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_extracted_entry(object name, object source, object entry_name,
                                                               object filter1 = null,
                                                               object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string entryName = PythonValueConverter.ToText(entry_name, "entry_name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFromExtractedEntry(nameText, source, entryName),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Extracted Entry history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_extracted_entry",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_extracted_entry(
            object old_name, object name, object source, object entry_name,
            object filter1 = null, object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string entryName = PythonValueConverter.ToText(entry_name, "entry_name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFromExtractedEntry(nameText, source, entryName),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates an XY history output and adds it to the current result, in one call.",
                   Arguments = "name: str, x_source: HistoryComponent or list[str], " +
                               "y_source: HistoryComponent or list[str], filter1: HistoryOutputFilter = None, " +
                               "filter2: HistoryOutputFilter = None", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_xy(object name, object x_source, object y_source,
                                             object filter1 = null,
                                             object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateXY(nameText, x_source, y_source), filter1Value, filter2Value));
        }

        [PythonAPI("Replaces an XY history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_xy", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_xy(object old_name, object name, object x_source, object y_source,
                                                 object filter1 = null,
                                                 object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateXY(nameText, x_source, y_source),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates a From Table history output and adds it to the current result, in one call.",
                   Arguments = "name: str, data_points: list[list[float]], " +
                               "y_column_names: list[str] = None, x_axis_unit: str = '/', " +
                               "y_axis_unit: str = '/', filter1: HistoryOutputFilter = None, " +
                               "filter2: HistoryOutputFilter = None", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_table(object name, object data_points,
                                                     object y_column_names = null, object x_axis_unit = null,
                                                     object y_axis_unit = null,
                                                     object filter1 = null,
                                                     object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string xAxisUnit = PythonValueConverter.ToTextOrDefault(x_axis_unit, "/", "x_axis_unit");
            string yAxisUnit = PythonValueConverter.ToTextOrDefault(y_axis_unit, "/", "y_axis_unit");
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFromTable(nameText, data_points, y_column_names, xAxisUnit,
                                                            yAxisUnit), filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a From Table history output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_table",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_table(object old_name, object name, object data_points,
                                                         object y_column_names = null,
                                                         object x_axis_unit = null, object y_axis_unit = null,
                                                         object filter1 = null,
                                                         object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string xAxisUnit = PythonValueConverter.ToTextOrDefault(x_axis_unit, "/", "x_axis_unit");
            string yAxisUnit = PythonValueConverter.ToTextOrDefault(y_axis_unit, "/", "y_axis_unit");
            ResultHistoryOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter1, "filter1", true);
            ResultHistoryOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultHistoryOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFromTable(nameText, data_points, y_column_names,
                                                                          xAxisUnit, yAxisUnit),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Validates and adds a result history-output draft using command history.",
                   Arguments = "output: HistoryOutputDraft", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add(object output)
        {
            ResultHistoryOutputDraftAPI outputValue =
                PythonValueConverter.ToApiObject<ResultHistoryOutputDraftAPI>(output, "output", false);
            string name = _service.AddHistoryOutput(_resultName, outputValue);
            return this[name];
        }

        [PythonAPI("Validates and replaces a result history-output definition using command history. The " +
                   "draft must be of the type the replaced output is made by.",
                   Arguments = "old_name: str, output: HistoryOutputDraft", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace(object old_name, object output)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            ResultHistoryOutputDraftAPI outputValue =
                PythonValueConverter.ToApiObject<ResultHistoryOutputDraftAPI>(output, "output", false);
            string name = _service.ReplaceHistoryOutput(_resultName, oldName, outputValue);
            return this[name];
        }

        [PythonAPI("Renames a user-defined result history output using command history. Solver-generated " +
                   "history outputs cannot be renamed.", Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(object old_name, object new_name)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string newName = PythonValueConverter.ToText(new_name, "new_name", false);
            _service.RenameHistoryOutput(_resultName, oldName, newName);
        }

        [PythonAPI("Validates and duplicates user-defined result history outputs using command history. " +
                   "Solver-generated history outputs cannot be duplicated.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            _service.DuplicateHistoryOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes result history outputs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            _service.RemoveHistoryOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes fields from a result history output using command history.",
                   Arguments = "history_output_name: str, field_names: list[str]", Returns = "None")]
        public void remove_fields(object history_output_name, object field_names)
        {
            string historyOutputName = PythonValueConverter.ToText(history_output_name, "history_output_name", false);
            _service.RemoveHistoryFields(_resultName, historyOutputName, field_names);
        }

        [PythonAPI("Validates and removes components from a result history-output field using command history.",
                   Arguments = "history_output_name: str, field_name: str, component_names: list[str]",
                   Returns = "None")]
        public void remove_components(object history_output_name, object field_name, object component_names)
        {
            string historyOutputName = PythonValueConverter.ToText(history_output_name, "history_output_name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            _service.RemoveHistoryComponents(_resultName, historyOutputName, fieldName, component_names);
        }

        [PythonAPI("Validates and exports all fields and components of the selected result history outputs to CSV " +
                   "using command history.",
                   Arguments = "file_name: str, history_output_names: list[str], delimiter: str = ','",
                   Returns = "None")]
        public void export_to_csv(object file_name, object history_output_names, object delimiter = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string delimiterText = PythonValueConverter.ToTextOrDefault(delimiter, ",", "delimiter");
            _service.ExportHistoryOutputsToCsv(_resultName, fileName, history_output_names, delimiterText);
        }

        private static void ApplyFromFieldOptions(ResultHistoryOutputFromFieldDraftAPI draft, string complexResultType,
                                                  object complexAngleDeg, object stepId, object incrementId,
                                                  object harmonic, string outputNodeCoordinates)
        {
            if (complexResultType != null) draft.complex_result_type = complexResultType;
            if (complexAngleDeg != null)
                draft.complex_angle_deg = PythonValueConverter.ToFiniteDouble(complexAngleDeg, "complex_angle_deg");
            if (stepId != null) draft.step_id = PythonValueConverter.ToInt(stepId, "step_id");
            if (incrementId != null) draft.increment_id = PythonValueConverter.ToInt(incrementId, "increment_id");
            if (harmonic != null) draft.harmonic = PythonValueConverter.ToBool(harmonic, "harmonic");
            if (outputNodeCoordinates != null) draft.output_node_coordinates = outputNodeCoordinates;
        }
        private static TDraft WithFilters<TDraft>(TDraft draft, ResultHistoryOutputFilterAPI filter1,
                                                  ResultHistoryOutputFilterAPI filter2)
            where TDraft : ResultHistoryOutputDraftAPI
        {
            if (filter1 != null) draft.filter1 = filter1;
            if (filter2 != null) draft.filter2 = filter2;
            //
            return draft;
        }
        public override string ToString()
        {
            string[] names = _dataService.GetHistoryOutputNames(_resultName);
            return Format(names, false);
        }
        protected override string ToFullString()
        {
            string[] names = _dataService.GetHistoryOutputNames(_resultName);
            return Format(names, true);
        }
        private static string Format(string[] names, bool full)
        {
            StringListAPI list = new StringListAPI(names);
            return "HistoryOutputs(count=" + names.Length + ", names=" +
                   (full ? list.GetFullDisplayString() : list.ToString()) + ")";
        }
        // A filter option is a name the results keep rather than an enumeration, so it is parsed as one whose
        // names are those the results keep
        private static ResultHistoryOutputFilterAPI CreateMinMaxFilter(HistoryResultFilterTypeEnum type,
                                                                        string returnValue)
        {
            HistoryFilterReturnValueEnum option =
                PythonValueConverter.ToEnum<HistoryFilterReturnValueEnum>(returnValue, "return_value");
            return new ResultHistoryOutputFilterAPI(type, option == HistoryFilterReturnValueEnum.Row
                                                          ? HistoryResultFilter.Row : HistoryResultFilter.Column);
        }
        private static ResultHistoryOutputFilterAPI CreateAggregateFilter(HistoryResultFilterTypeEnum type,
                                                                           string operateOn)
        {
            HistoryFilterOperateOnEnum option =
                PythonValueConverter.ToEnum<HistoryFilterOperateOnEnum>(operateOn, "operate_on");
            return new ResultHistoryOutputFilterAPI(type, option == HistoryFilterOperateOnEnum.Rows
                                                          ? HistoryResultFilter.Rows : HistoryResultFilter.Columns);
        }
    }
}
