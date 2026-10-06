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
        public ResultHistoryOutputAPI __getitem__(string historyOutputName)
        {
            return this[historyOutputName];
        }
        public ResultHistoryOutputAPI this[string historyOutputName]
        {
            get
            {
                _dataService.ResolveHistoryOutput(_resultName, historyOutputName);
                return _dataService.CreateAPI(_resultName, historyOutputName);
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
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<ResultHistoryOutputAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(string historyOutputName)
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
        public bool contains(string name)
        {
            return _dataService.GetHistoryOutputNames(_resultName).Contains(name);
        }

        [PythonAPI("Creates a From Field result history-output draft. The region can be a result node selection, " +
                   "a NodeSetName or SurfaceName region created by pmx.create.region and resolved against the " +
                   "result mesh, or a result node set or surface. This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_names: list[str], " +
                               "region: Selection or Region or NodeSet or Surface",
                   Returns = "HistoryOutputFromFieldDraft")]
        public ResultHistoryOutputFromFieldDraftAPI create_history_output_from_field(
            string name, string field_name, object component_names, object region)
        {
            return _service.CreateFromField(name, field_name, component_names, region);
        }

        [PythonAPI("Creates a From History Output by Equation draft. This does not modify the result.",
                   Arguments = "name: str, equation: str, unit: str = '/'",
                   Returns = "HistoryOutputFromEquationDraft")]
        public ResultHistoryOutputFromEquationDraftAPI create_history_output_from_equation(
            string name, string equation, string unit = "/")
        {
            return _service.CreateFromEquation(name, equation, unit);
        }

        [PythonAPI("Creates a From Integration draft. Possible integral_type values are Line, Surface and " +
                   "Volume. Line integration accepts an edges-target selection; surface integration accepts a " +
                   "surface-target selection, a SurfaceName region or a result surface; volume integration accepts " +
                   "an element-target selection, an ElementSetName region or a result element set, where a " +
                   "region created by pmx.create.region is resolved against the result mesh. Possible " +
                   "integral_result values are Geometry (the region length/area/volume), Field (the field " +
                   "integral) and FieldByGeometry (the region average of the field); field_name and component_names are ignored for Geometry. The cross_section " +
                   "scaling (None, Thickness or Axisymmetric) applies to Line and Volume integrals of " +
                   "2D output, and Thickness additionally uses the thickness argument. Possible " +
                   "deformation_variable_name values are 'Displacements', " +
                   "'Forces', 'Surface Normals', 'Wear Depths', 'Mesh Deformation', 'Disp&Def', and " +
                   "'Disp&Def&Depth'.",
                   Arguments = "name: str, integral_type: str, field_name: str = None, " +
                               "component_names: list[str] = None, " +
                               "region: Selection or Region or ElementSet or Surface = None, " +
                               "integral_result: str = 'Geometry', " +
                               "deformation_variable_name: str = 'Displacements', " +
                               "unit: str = '/', cross_section: str = 'None', thickness: float = 1.0",
                   Returns = "HistoryOutputFromIntegrationDraft")]
        public ResultHistoryOutputFromIntegrationDraftAPI create_history_output_from_integration(
            string name, string integral_type, string field_name = null, object component_names = null,
            object region = null, string integral_result = "Geometry",
            string deformation_variable_name = "Displacements", string unit = "/",
            string cross_section = "None", double thickness = 1.0)
        {
            return _service.CreateFromIntegration(name, integral_type, field_name, component_names, region,
                                                  integral_result, deformation_variable_name, unit,
                                                  cross_section, thickness);
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
            string name, object source1, object source2, object source3 = null, object source4 = null)
        {
            return _service.CreateFromMergedHistories(name, source1, source2, source3, source4);
        }

        [PythonAPI("Creates a From Extracted Entry draft that extracts one named entry of a history component into " +
                   "a separate history output. The source is a HistoryComponent or a list of three strings " +
                   "[history_output_name, field_name, component_name]. This does not modify the result.",
                   Arguments = "name: str, source: HistoryComponent or list[str], entry_name: str",
                   Returns = "HistoryOutputFromExtractedEntryDraft")]
        public ResultHistoryOutputFromExtractedEntryDraftAPI create_history_output_from_extracted_entry(
            string name, object source, string entry_name)
        {
            return _service.CreateFromExtractedEntry(name, source, entry_name);
        }

        [PythonAPI("Creates an XY draft that plots the entries of the Y history component against the values of " +
                   "the X history component. Each source is a HistoryComponent or a list of three strings " +
                   "[history_output_name, field_name, component_name]. The X component must contain exactly one " +
                   "entry, and the sources must share a common reference time. This does not modify the result.",
                   Arguments = "name: str, x_source: HistoryComponent or list[str], " +
                               "y_source: HistoryComponent or list[str]",
                   Returns = "HistoryOutputXYDraft")]
        public ResultHistoryOutputXYDraftAPI create_history_output_xy(string name, object x_source, object y_source)
        {
            return _service.CreateXY(name, x_source, y_source);
        }

        [PythonAPI("Creates a From Table draft from user-defined data. Every row of data_points holds the X value " +
                   "followed by one value per Y column, and all rows must be of equal length. The number of Y " +
                   "columns is taken from the row length; y_column_names defaults to 'Y1', 'Y2', ... " +
                   "This does not modify the result.",
                   Arguments = "name: str, data_points: list[list[float]], y_column_names: list[str] = None, " +
                               "x_axis_unit: str = '/', y_axis_unit: str = '/'",
                   Returns = "HistoryOutputFromTableDraft")]
        public ResultHistoryOutputFromTableDraftAPI create_history_output_from_table(
            string name, object data_points, object y_column_names = null, string x_axis_unit = "/",
            string y_axis_unit = "/")
        {
            return _service.CreateFromTable(name, data_points, y_column_names, x_axis_unit, y_axis_unit);
        }

        [PythonAPI("Creates a minimum history-output filter. Possible return_value values are Row and Column.",
                   Arguments = "return_value: str = 'Column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_minimum_filter(string return_value = "Column")
        {
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Minimum, return_value);
        }

        [PythonAPI("Creates a maximum history-output filter. Possible return_value values are Row and Column.",
                   Arguments = "return_value: str = 'Column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_maximum_filter(string return_value = "Column")
        {
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Maximum, return_value);
        }

        [PythonAPI("Creates a sum history-output filter. Possible operate_on values are Rows and Columns.",
                   Arguments = "operate_on: str = 'Columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_sum_filter(string operate_on = "Columns")
        {
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Sum, operate_on);
        }

        [PythonAPI("Creates an average history-output filter. Possible operate_on values are Rows and Columns.",
                   Arguments = "operate_on: str = 'Columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_average_filter(string operate_on = "Columns")
        {
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Average, operate_on);
        }

        [PythonAPI("Creates a From Field history output and adds it to the current result, in one call. " +
                   "It takes the arguments of create_history_output_from_field, the options of its draft and " +
                   "the optional filters. An option that is not given keeps the default of the draft.",
                   Arguments = "name: str, field_name: str, component_names: list[str], " +
                               "region: Selection or Region or NodeSet or Surface, complex_result_type: str = None, " +
                               "complex_angle_deg: float = None, step_id: int = None, increment_id: int = None, " +
                               "harmonic: bool = None, output_node_coordinates: str = None, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_field(string name, string field_name, object component_names,
                                                     object region, string complex_result_type = null,
                                                     object complex_angle_deg = null, object step_id = null,
                                                     object increment_id = null, object harmonic = null,
                                                     string output_node_coordinates = null,
                                                     ResultHistoryOutputFilterAPI filter1 = null,
                                                     ResultHistoryOutputFilterAPI filter2 = null)
        {
            ResultHistoryOutputFromFieldDraftAPI draft = _service.CreateFromField(name, field_name, component_names,
                                                                                  region);
            ApplyFromFieldOptions(draft, complex_result_type, complex_angle_deg, step_id, increment_id, harmonic,
                                  output_node_coordinates);
            return add(WithFilters(draft, filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Field one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_field",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_field(string old_name, string name, string field_name,
                                                         object component_names, object region,
                                                         string complex_result_type = null,
                                                         object complex_angle_deg = null, object step_id = null,
                                                         object increment_id = null, object harmonic = null,
                                                         string output_node_coordinates = null,
                                                         ResultHistoryOutputFilterAPI filter1 = null,
                                                         ResultHistoryOutputFilterAPI filter2 = null)
        {
            ResultHistoryOutputFromFieldDraftAPI draft = _service.CreateFromField(name, field_name, component_names,
                                                                                  region);
            ApplyFromFieldOptions(draft, complex_result_type, complex_angle_deg, step_id, increment_id, harmonic,
                                  output_node_coordinates);
            return replace(old_name, WithFilters(draft, filter1, filter2));
        }

        [PythonAPI("Creates a From Equation history output and adds it to the current result, in one call.",
                   Arguments = "name: str, equation: str, unit: str = '/', " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_equation(string name, string equation, string unit = "/",
                                                        ResultHistoryOutputFilterAPI filter1 = null,
                                                        ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFromEquation(name, equation, unit), filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Equation one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_equation",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_equation(string old_name, string name, string equation,
                                                            string unit = "/",
                                                            ResultHistoryOutputFilterAPI filter1 = null,
                                                            ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFromEquation(name, equation, unit),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates a From Integration history output and adds it to the current result, in one " +
                   "call. It takes the arguments of create_history_output_from_integration plus the " +
                   "optional filters.",
                   Arguments = "name: str, integral_type: str, and the arguments of " +
                               "create_history_output_from_integration", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_integration(
            string name, string integral_type, string field_name = null, object component_names = null,
            object region = null, string integral_result = "Geometry",
            string deformation_variable_name = "Displacements", string unit = "/",
            string cross_section = "None", double thickness = 1.0,
            ResultHistoryOutputFilterAPI filter1 = null, ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFromIntegration(name, integral_type, field_name,
                                                                   component_names, region, integral_result,
                                                                   deformation_variable_name, unit,
                                                                   cross_section, thickness),
                                   filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Integration one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_integration",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_integration(
            string old_name, string name, string integral_type, string field_name = null,
            object component_names = null, object region = null, string integral_result = "Geometry",
            string deformation_variable_name = "Displacements", string unit = "/",
            string cross_section = "None", double thickness = 1.0,
            ResultHistoryOutputFilterAPI filter1 = null, ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFromIntegration(name, integral_type, field_name,
                                                                                component_names, region,
                                                                                integral_result,
                                                                                deformation_variable_name,
                                                                                unit, cross_section, thickness),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates a From Merged Histories history output and adds it to the current result, in " +
                   "one call.",
                   Arguments = "name: str, source1, source2, source3 = None, source4 = None, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_merged_histories(string name, object source1, object source2,
                                                                object source3 = null, object source4 = null,
                                                                ResultHistoryOutputFilterAPI filter1 = null,
                                                                ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFromMergedHistories(name, source1, source2, source3, source4),
                                   filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Merged Histories one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_merged_histories",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_merged_histories(
            string old_name, string name, object source1, object source2, object source3 = null,
            object source4 = null, ResultHistoryOutputFilterAPI filter1 = null,
            ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFromMergedHistories(name, source1, source2,
                                                                                    source3, source4),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates a From Extracted Entry history output and adds it to the current result, in " +
                   "one call.",
                   Arguments = "name: str, source, entry_name: str, " +
                               "filter1: HistoryOutputFilter = None, filter2: HistoryOutputFilter = None",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_extracted_entry(string name, object source, string entry_name,
                                                               ResultHistoryOutputFilterAPI filter1 = null,
                                                               ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFromExtractedEntry(name, source, entry_name),
                                   filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Extracted Entry one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_extracted_entry",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_extracted_entry(
            string old_name, string name, object source, string entry_name,
            ResultHistoryOutputFilterAPI filter1 = null, ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFromExtractedEntry(name, source, entry_name),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates an XY history output and adds it to the current result, in one call.",
                   Arguments = "name: str, x_source, y_source, filter1: HistoryOutputFilter = None, " +
                               "filter2: HistoryOutputFilter = None", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_xy(string name, object x_source, object y_source,
                                             ResultHistoryOutputFilterAPI filter1 = null,
                                             ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateXY(name, x_source, y_source), filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by an XY one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_xy", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_xy(string old_name, string name, object x_source, object y_source,
                                                 ResultHistoryOutputFilterAPI filter1 = null,
                                                 ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateXY(name, x_source, y_source),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates a From Table history output and adds it to the current result, in one call.",
                   Arguments = "name: str, data_points: list[list[float]], " +
                               "y_column_names: list[str] = None, x_axis_unit: str = '/', " +
                               "y_axis_unit: str = '/', filter1: HistoryOutputFilter = None, " +
                               "filter2: HistoryOutputFilter = None", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add_from_table(string name, object data_points,
                                                     object y_column_names = null, string x_axis_unit = "/",
                                                     string y_axis_unit = "/",
                                                     ResultHistoryOutputFilterAPI filter1 = null,
                                                     ResultHistoryOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFromTable(name, data_points, y_column_names, x_axis_unit,
                                                            y_axis_unit), filter1, filter2));
        }

        [PythonAPI("Replaces a result history output by a From Table one, in one call.",
                   Arguments = "old_name: str, and the arguments of add_from_table",
                   Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace_from_table(string old_name, string name, object data_points,
                                                         object y_column_names = null,
                                                         string x_axis_unit = "/", string y_axis_unit = "/",
                                                         ResultHistoryOutputFilterAPI filter1 = null,
                                                         ResultHistoryOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFromTable(name, data_points, y_column_names,
                                                                          x_axis_unit, y_axis_unit),
                                                 filter1, filter2));
        }

        [PythonAPI("Validates and adds a result history-output draft using command history.",
                   Arguments = "output: HistoryOutputDraft", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI add(ResultHistoryOutputDraftAPI output)
        {
            string name = _service.AddHistoryOutput(_resultName, output);
            return this[name];
        }

        [PythonAPI("Validates and replaces a result history-output definition using command history.",
                   Arguments = "old_name: str, output: HistoryOutputDraft", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI replace(string old_name, ResultHistoryOutputDraftAPI output)
        {
            string name = _service.ReplaceHistoryOutput(_resultName, old_name, output);
            return this[name];
        }

        [PythonAPI("Renames a user-defined result history output using command history. Solver-generated " +
                   "history outputs cannot be renamed.", Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            _service.RenameHistoryOutput(_resultName, old_name, new_name);
        }

        [PythonAPI("Validates and duplicates user-defined result history outputs using command history. " +
                   "Solver-generated history outputs cannot be duplicated.",
                   Arguments = "names: list[str]", Returns = "None")]
        public void duplicate(object names)
        {
            _service.DuplicateHistoryOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes result history outputs using command history.",
                   Arguments = "names: list[str]", Returns = "None")]
        public void remove(object names)
        {
            _service.RemoveHistoryOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes fields from a result history output using command history.",
                   Arguments = "history_output_name: str, field_names: list[str]", Returns = "None")]
        public void remove_fields(string history_output_name, object field_names)
        {
            _service.RemoveHistoryFields(_resultName, history_output_name, field_names);
        }

        [PythonAPI("Validates and removes components from a result history-output field using command history.",
                   Arguments = "history_output_name: str, field_name: str, component_names: list[str]",
                   Returns = "None")]
        public void remove_components(string history_output_name, string field_name, object component_names)
        {
            _service.RemoveHistoryComponents(_resultName, history_output_name, field_name, component_names);
        }

        [PythonAPI("Validates and exports all fields and components of the selected result history outputs to CSV " +
                   "using command history.",
                   Arguments = "file_name: str, history_output_names: list[str], delimiter: str = ','",
                   Returns = "None")]
        public void export_to_csv(string file_name, object history_output_names, string delimiter = ",")
        {
            _service.ExportHistoryOutputsToCsv(_resultName, file_name, history_output_names, delimiter);
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
        private static ResultHistoryOutputFilterAPI CreateMinMaxFilter(HistoryResultFilterTypeEnum type,
                                                                        string returnValue)
        {
            string option = ToFilterOption(returnValue, "return_value", HistoryResultFilter.Row,
                                           HistoryResultFilter.Column);
            return new ResultHistoryOutputFilterAPI(type, option);
        }
        private static ResultHistoryOutputFilterAPI CreateAggregateFilter(HistoryResultFilterTypeEnum type,
                                                                           string operateOn)
        {
            string option = ToFilterOption(operateOn, "operate_on", HistoryResultFilter.Rows,
                                           HistoryResultFilter.Columns);
            return new ResultHistoryOutputFilterAPI(type, option);
        }
        // A filter option is a name of the results rather than an enumeration, so it is matched here the
        // way ToEnum matches one: ignoring case, and read back in the spelling the results keep
        private static string ToFilterOption(string value, string argumentName, params string[] options)
        {
            if (value == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            foreach (string option in options)
            {
                if (string.Equals(option, value.Trim(), System.StringComparison.OrdinalIgnoreCase)) return option;
            }
            throw new PythonAPIException("The value '" + value + "' is not supported for the argument '" +
                                         argumentName + "'. Possible values are " + string.Join(", ", options) +
                                         ".");
        }
    }
}
