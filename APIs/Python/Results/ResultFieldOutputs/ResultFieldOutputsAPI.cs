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

namespace APIs.Python
{
    public class ResultFieldOutputsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly ResultFieldOutputService _service;


        // Constructors
        internal ResultFieldOutputsAPI(string resultName, ResultFieldOutputService service)
        {
            _resultName = resultName;
            _service = service;
        }


        // Methods
        public ResultFieldOutputAPI __getitem__(string fieldOutputName)
        {
            return this[fieldOutputName];
        }
        public ResultFieldOutputAPI this[string fieldOutputName]
        {
            get { return _service.CreateAPI(_resultName, fieldOutputName); }
        }
        public int __len__()
        {
            return _service.GetNames(_resultName).Length;
        }
        public bool __contains__(string fieldOutputName)
        {
            return contains(fieldOutputName);
        }

        [PythonAPI("Returns result field output names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_service.GetNames(_resultName));
        }

        [PythonAPI("Returns the first result field output.", Returns = "FieldOutput")]
        public ResultFieldOutputAPI first()
        {
            string[] names = _service.GetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The field outputs collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result field output.", Returns = "FieldOutput")]
        public ResultFieldOutputAPI last()
        {
            string[] names = _service.GetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The field outputs collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result field output exists.",
                   Arguments = "field_output_name: str", Returns = "bool")]
        public bool contains(string fieldOutputName)
        {
            return _service.GetNames(_resultName).Contains(fieldOutputName);
        }

        [PythonAPI("Creates a limit field-output draft. Possible based_on values are 'parts', " +
                   "'element_sets', and 'all_elements'. For 'all_elements', item_limits must contain " +
                   "one item named 'All elements'. This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_name: str, based_on: str, " +
                               "item_limits: dict[str, float]",
                   Returns = "FieldOutputLimitDraft")]
        public ResultFieldOutputLimitDraftAPI create_field_output_limit(string name, string field_name,
                                                                        string component_name, string based_on,
                                                                        object item_limits)
        {
            return _service.CreateFieldOutputLimit(name, field_name, component_name, based_on, item_limits);
        }

        [PythonAPI("Creates an envelope field-output draft with MAX, MIN, and AVERAGE components. " +
                   "This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_name: str",
                   Returns = "FieldOutputEnvelopeDraft")]
        public ResultFieldOutputEnvelopeDraftAPI create_field_output_envelope(string name, string field_name,
                                                                               string component_name)
        {
            return _service.CreateFieldOutputEnvelope(name, field_name, component_name);
        }

        [PythonAPI("Creates an equation field-output draft. Field references use FIELD.COMPONENT and are " +
                   "case-sensitive. This does not modify the result.",
                   Arguments = "name: str, equation: str, unit: str = '/'",
                   Returns = "FieldOutputEquationDraft")]
        public ResultFieldOutputEquationDraftAPI create_field_output_equation(string name, string equation,
                                                                               string unit = "/")
        {
            return _service.CreateFieldOutputEquation(name, equation, unit);
        }

        [PythonAPI("Creates a coordinate-system transform field-output draft for a complete vector or tensor " +
                   "field. This does not modify the result.",
                   Arguments = "name: str, field_name: str, coordinate_system_name: str",
                   Returns = "FieldOutputCoordinateSystemTransformDraft")]
        public ResultFieldOutputCoordinateSystemTransformDraftAPI create_field_output_coordinate_system_transform(
            string name, string field_name, string coordinate_system_name)
        {
            return _service.CreateFieldOutputCoordinateSystemTransform(name, field_name, coordinate_system_name);
        }

        [PythonAPI("Creates a median field-output filter. Possible source values are " +
                   "'all_nodes' and 'surface_nodes'.",
                   Arguments = "source: str = 'all_nodes', layers: int = 1, iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_median_filter(string source = "all_nodes", int layers = 1,
                                                               int iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Median, source, layers, iterations);
        }

        [PythonAPI("Creates an average field-output filter. Possible source values are " +
                   "'all_nodes' and 'surface_nodes'.",
                   Arguments = "source: str = 'all_nodes', layers: int = 1, iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_average_filter(string source = "all_nodes", int layers = 1,
                                                                int iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Average, source, layers, iterations);
        }

        [PythonAPI("Creates a smooth field-output filter. Possible source values are " +
                   "'all_nodes' and 'surface_nodes'.",
                   Arguments = "source: str = 'all_nodes', layers: int = 1, iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_smooth_filter(string source = "all_nodes", int layers = 1,
                                                               int iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Smooth, source, layers, iterations);
        }

        [PythonAPI("Validates and adds a field-output draft to the current result.",
                   Arguments = "output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI add(ResultFieldOutputDraftAPI output)
        {
            return _service.AddFieldOutput(_resultName, output);
        }

        [PythonAPI("Validates and replaces a result field-output definition using command history.",
                   Arguments = "old_name: str, output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace(string old_name, ResultFieldOutputDraftAPI output)
        {
            return _service.ReplaceFieldOutput(_resultName, old_name, output);
        }

        [PythonAPI("Validates and removes result field outputs using command history.",
                   Arguments = "field_output_names: list[str]", Returns = "None")]
        public void remove(object field_output_names)
        {
            _service.RemoveFieldOutputs(_resultName, field_output_names);
        }

        [PythonAPI("Validates and removes components from a result field output using command history.",
                   Arguments = "field_output_name: str, component_names: list[str]", Returns = "None")]
        public void remove_components(string field_output_name, object component_names)
        {
            _service.RemoveFieldOutputComponents(_resultName, field_output_name, component_names);
        }

        [PythonAPI("Validates and exports all components of a result field output to CSV using command history.",
                   Arguments = "file_name: str, field_output_name: str, step_id: int, increment_id: int",
                   Returns = "None")]
        public void export_to_csv(string file_name, string field_output_name, int step_id, int increment_id)
        {
            _service.ExportFieldOutputToCsv(_resultName, file_name, field_output_name, step_id, increment_id);
        }
        public override string ToString()
        {
            string[] names = _service.GetNames(_resultName);
            return "FieldOutputs(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _service.GetNames(_resultName);
            return "FieldOutputs(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
        private static ResultFieldOutputFilterAPI CreateFilter(FieldResultFilterTypeEnum type, string source,
                                                                int layers, int iterations)
        {
            FieldResultFilterSourceTypeEnum sourceType;
            if (source == "all_nodes") sourceType = FieldResultFilterSourceTypeEnum.AllNodes;
            else if (source == "surface_nodes") sourceType = FieldResultFilterSourceTypeEnum.SurfaceNodes;
            else throw new PythonAPIException("The argument 'source' must be 'all_nodes' or 'surface_nodes'.");
            if (layers < 1) throw new PythonAPIException("The argument 'layers' must be at least 1.");
            if (iterations < 1) throw new PythonAPIException("The argument 'iterations' must be at least 1.");
            return new ResultFieldOutputFilterAPI(type, sourceType, layers, iterations);
        }
    }
}
