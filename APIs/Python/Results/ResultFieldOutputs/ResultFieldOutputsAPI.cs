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
    public class ResultFieldOutputsAPI : BaseAPI, IReadOnlyList<ResultFieldOutputAPI>
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
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultFieldOutputAPI IReadOnlyList<ResultFieldOutputAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().ToArray();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<ResultFieldOutputAPI> GetEnumerator()
        {
            foreach (string key in get_names().ToArray()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
                   Arguments = "name: str", Returns = "bool")]
        public bool contains(string name)
        {
            return PythonNameComparer.Contains(_service.GetNames(_resultName), name);
        }

        [PythonAPI("Creates a limit field-output draft. Possible based_on values are Parts, ElementSets " +
                   "and AllElements. For AllElements, item_limits must contain one item named 'All elements'. " +
                   "Part and element set names are matched regardless of case and stored as the result " +
                   "spells them; two keys naming the same item are refused when the output is added. " +
                   "This does not modify the result.",
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

        [PythonAPI("Creates a median field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_median_filter(string source = "AllNodes",
                                                               int number_of_layers = 1,
                                                               int number_of_iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Median, source, number_of_layers, number_of_iterations);
        }

        [PythonAPI("Creates an average field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_average_filter(string source = "AllNodes",
                                                                int number_of_layers = 1,
                                                                int number_of_iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Average, source, number_of_layers, number_of_iterations);
        }

        [PythonAPI("Creates a smooth field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_smooth_filter(string source = "AllNodes",
                                                               int number_of_layers = 1,
                                                               int number_of_iterations = 1)
        {
            return CreateFilter(FieldResultFilterTypeEnum.Smooth, source, number_of_layers, number_of_iterations);
        }

        [PythonAPI("Creates a limit field output and adds it to the current result, in one call. It takes " +
                   "the arguments of create_field_output_limit plus the optional filters.",
                   Arguments = "name: str, field_name: str, component_name: str, based_on: str, " +
                               "item_limits: dict[str, float], filter1: FieldOutputFilter = None, " +
                               "filter2: FieldOutputFilter = None", Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_limit(string name, string field_name, string component_name,
                                              string based_on, object item_limits,
                                              ResultFieldOutputFilterAPI filter1 = null,
                                              ResultFieldOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFieldOutputLimit(name, field_name, component_name, based_on,
                                                                    item_limits), filter1, filter2));
        }

        [PythonAPI("Replaces a result field output by a limit field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_limit", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_limit(string old_name, string name, string field_name,
                                                  string component_name, string based_on, object item_limits,
                                                  ResultFieldOutputFilterAPI filter1 = null,
                                                  ResultFieldOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFieldOutputLimit(name, field_name,
                                                                                 component_name, based_on,
                                                                                 item_limits),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates an envelope field output and adds it to the current result, in one call.",
                   Arguments = "name: str, field_name: str, component_name: str, " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_envelope(string name, string field_name, string component_name,
                                                 ResultFieldOutputFilterAPI filter1 = null,
                                                 ResultFieldOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFieldOutputEnvelope(name, field_name, component_name),
                                   filter1, filter2));
        }

        [PythonAPI("Replaces a result field output by an envelope field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_envelope", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_envelope(string old_name, string name, string field_name,
                                                     string component_name,
                                                     ResultFieldOutputFilterAPI filter1 = null,
                                                     ResultFieldOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFieldOutputEnvelope(name, field_name,
                                                                                    component_name),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates an equation field output and adds it to the current result, in one call.",
                   Arguments = "name: str, equation: str, unit: str = '/', " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput",
                   Example = "pmx.results.current.field_outputs.add_equation('SEQV2', 'STRESS.MISES * 2')")]
        public ResultFieldOutputAPI add_equation(string name, string equation, string unit = "/",
                                                 ResultFieldOutputFilterAPI filter1 = null,
                                                 ResultFieldOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFieldOutputEquation(name, equation, unit), filter1, filter2));
        }

        [PythonAPI("Replaces a result field output by an equation field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_equation", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_equation(string old_name, string name, string equation,
                                                     string unit = "/",
                                                     ResultFieldOutputFilterAPI filter1 = null,
                                                     ResultFieldOutputFilterAPI filter2 = null)
        {
            return replace(old_name, WithFilters(_service.CreateFieldOutputEquation(name, equation, unit),
                                                 filter1, filter2));
        }

        [PythonAPI("Creates a coordinate system transform field output and adds it to the current result, " +
                   "in one call.",
                   Arguments = "name: str, field_name: str, coordinate_system_name: str, " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_coordinate_system_transform(string name, string field_name,
                                                                    string coordinate_system_name,
                                                                    ResultFieldOutputFilterAPI filter1 = null,
                                                                    ResultFieldOutputFilterAPI filter2 = null)
        {
            return add(WithFilters(_service.CreateFieldOutputCoordinateSystemTransform(name, field_name,
                                                                                        coordinate_system_name),
                                   filter1, filter2));
        }

        [PythonAPI("Replaces a result field output by a coordinate system transform field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_coordinate_system_transform",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_coordinate_system_transform(
            string old_name, string name, string field_name, string coordinate_system_name,
            ResultFieldOutputFilterAPI filter1 = null, ResultFieldOutputFilterAPI filter2 = null)
        {
            return replace(old_name,
                           WithFilters(_service.CreateFieldOutputCoordinateSystemTransform(
                                           name, field_name, coordinate_system_name), filter1, filter2));
        }

        [PythonAPI("Validates and adds a field-output draft to the current result.",
                   Arguments = "output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI add(ResultFieldOutputDraftAPI output)
        {
            return _service.AddFieldOutput(_resultName, output);
        }

        [PythonAPI("Validates and replaces a result field-output definition using command history. The draft " +
                   "must be of the type the replaced output is made by.",
                   Arguments = "old_name: str, output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace(string old_name, ResultFieldOutputDraftAPI output)
        {
            return _service.ReplaceFieldOutput(_resultName, old_name, output);
        }

        [PythonAPI("Renames a result field output using command history.",
                   Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            _service.RenameFieldOutput(_resultName, old_name, new_name);
        }

        [PythonAPI("Duplicates result field outputs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            _service.DuplicateFieldOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes result field outputs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            _service.RemoveFieldOutputs(_resultName, names);
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
        private static TDraft WithFilters<TDraft>(TDraft draft, ResultFieldOutputFilterAPI filter1,
                                                  ResultFieldOutputFilterAPI filter2)
            where TDraft : ResultFieldOutputDraftAPI
        {
            if (filter1 != null) draft.filter1 = filter1;
            if (filter2 != null) draft.filter2 = filter2;
            //
            return draft;
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
            FieldResultFilterSourceTypeEnum sourceType =
                PythonValueConverter.ToEnum<FieldResultFilterSourceTypeEnum>(source, "source");
            if (layers < 1) throw new PythonAPIException("The argument 'number_of_layers' must be at least 1.");
            if (iterations < 1)
                throw new PythonAPIException("The argument 'number_of_iterations' must be at least 1.");
            return new ResultFieldOutputFilterAPI(type, sourceType, layers, iterations);
        }
    }
}
