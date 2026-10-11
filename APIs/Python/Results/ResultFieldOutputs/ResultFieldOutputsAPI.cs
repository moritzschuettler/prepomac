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
        public ResultFieldOutputAPI __getitem__(object fieldOutputName)
        {
            string fieldOutputNameText = PythonValueConverter.ToText(fieldOutputName, "name", false);
            return this[fieldOutputNameText];
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
        public bool __contains__(object fieldOutputName)
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
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return PythonNameComparer.Contains(_service.GetNames(_resultName), nameText);
        }

        [PythonAPI("Creates a limit field-output draft. Possible based_on values are Parts, ElementSets " +
                   "and AllElements. For AllElements, item_limits must contain one item named 'All elements'. " +
                   "Part and element set names and the 'All elements' key are matched regardless of case and " +
                   "stored as the result spells them; two keys naming the same item are refused when the " +
                   "output is added. The source field_name and component_name are matched the same way. " +
                   "This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_name: str, based_on: str, " +
                               "item_limits: dict[str, float]",
                   Returns = "FieldOutputLimitDraft")]
        public ResultFieldOutputLimitDraftAPI create_field_output_limit(object name, object field_name,
                                                                        object component_name, object based_on,
                                                                        object item_limits)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            string basedOn = PythonValueConverter.ToText(based_on, "based_on", false);
            return _service.CreateFieldOutputLimit(nameText, fieldName, componentName, basedOn, item_limits);
        }

        [PythonAPI("Creates an envelope field-output draft with MAX, MIN, and AVERAGE components. The source " +
                   "field_name and component_name are matched regardless of case and stored as the result " +
                   "spells them. This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_name: str",
                   Returns = "FieldOutputEnvelopeDraft")]
        public ResultFieldOutputEnvelopeDraftAPI create_field_output_envelope(object name, object field_name,
                                                                               object component_name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            return _service.CreateFieldOutputEnvelope(nameText, fieldName, componentName);
        }

        [PythonAPI("Creates an equation field-output draft. Field references use FIELD.COMPONENT and are " +
                   "case-sensitive. This does not modify the result.",
                   Arguments = "name: str, equation: str, unit: str = '/'",
                   Returns = "FieldOutputEquationDraft")]
        public ResultFieldOutputEquationDraftAPI create_field_output_equation(
            object name, object equation, object unit = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            return _service.CreateFieldOutputEquation(nameText, equationText, unitText);
        }

        [PythonAPI("Creates a coordinate-system transform field-output draft for a complete vector or tensor " +
                   "field. The source field_name is matched regardless of case and stored as the result spells it. " +
                   "This does not modify the result.",
                   Arguments = "name: str, field_name: str, coordinate_system_name: str",
                   Returns = "FieldOutputCoordinateSystemTransformDraft")]
        public ResultFieldOutputCoordinateSystemTransformDraftAPI create_field_output_coordinate_system_transform(
            object name, object field_name, object coordinate_system_name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string coordinateSystemName =
                PythonValueConverter.ToText(coordinate_system_name, "coordinate_system_name", false);
            return _service.CreateFieldOutputCoordinateSystemTransform(nameText, fieldName, coordinateSystemName);
        }

        [PythonAPI("Creates a median field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_median_filter(
            object source = null, object number_of_layers = null,
            object number_of_iterations = null)
        {
            string sourceText = PythonValueConverter.ToTextOrDefault(source, "AllNodes", "source");
            return CreateFilter(FieldResultFilterTypeEnum.Median, sourceText, number_of_layers,
                                number_of_iterations);
        }

        [PythonAPI("Creates an average field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_average_filter(
            object source = null, object number_of_layers = null,
            object number_of_iterations = null)
        {
            string sourceText = PythonValueConverter.ToTextOrDefault(source, "AllNodes", "source");
            return CreateFilter(FieldResultFilterTypeEnum.Average, sourceText, number_of_layers,
                                number_of_iterations);
        }

        [PythonAPI("Creates a smooth field-output filter. Possible source values are AllNodes and " +
                   "SurfaceNodes.",
                   Arguments = "source: str = 'AllNodes', number_of_layers: int = 1, " +
                               "number_of_iterations: int = 1",
                   Returns = "FieldOutputFilter")]
        public ResultFieldOutputFilterAPI create_smooth_filter(
            object source = null, object number_of_layers = null,
            object number_of_iterations = null)
        {
            string sourceText = PythonValueConverter.ToTextOrDefault(source, "AllNodes", "source");
            return CreateFilter(FieldResultFilterTypeEnum.Smooth, sourceText, number_of_layers,
                                number_of_iterations);
        }

        [PythonAPI("Creates a limit field output and adds it to the current result, in one call. It takes " +
                   "the arguments of create_field_output_limit plus the optional filters.",
                   Arguments = "name: str, field_name: str, component_name: str, based_on: str, " +
                               "item_limits: dict[str, float], filter1: FieldOutputFilter = None, " +
                               "filter2: FieldOutputFilter = None", Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_limit(object name, object field_name, object component_name,
                                              object based_on, object item_limits,
                                              object filter1 = null,
                                              object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            string basedOn = PythonValueConverter.ToText(based_on, "based_on", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFieldOutputLimit(nameText, fieldName, componentName, basedOn,
                                                                    item_limits), filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a limit field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_limit", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_limit(object old_name, object name, object field_name,
                                                  object component_name, object based_on, object item_limits,
                                                  object filter1 = null,
                                                  object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            string basedOn = PythonValueConverter.ToText(based_on, "based_on", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFieldOutputLimit(nameText, fieldName,
                                                                                 componentName, basedOn,
                                                                                 item_limits),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates an envelope field output and adds it to the current result, in one call.",
                   Arguments = "name: str, field_name: str, component_name: str, " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_envelope(object name, object field_name, object component_name,
                                                 object filter1 = null,
                                                 object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFieldOutputEnvelope(nameText, fieldName, componentName),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces an envelope field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_envelope", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_envelope(object old_name, object name, object field_name,
                                                     object component_name,
                                                     object filter1 = null,
                                                     object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFieldOutputEnvelope(nameText, fieldName,
                                                                                    componentName),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates an equation field output and adds it to the current result, in one call.",
                   Arguments = "name: str, equation: str, unit: str = '/', " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput",
                   Example = "pmx.results.current.field_outputs.add_equation('SEQV2', 'STRESS.MISES * 2')")]
        public ResultFieldOutputAPI add_equation(object name, object equation,
                                                 object unit = null,
                                                 object filter1 = null,
                                                 object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFieldOutputEquation(nameText, equationText, unitText),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces an equation field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_equation", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_equation(object old_name, object name, object equation,
                                                     object unit = null,
                                                     object filter1 = null,
                                                     object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string equationText = PythonValueConverter.ToText(equation, "equation", false);
            string unitText = PythonValueConverter.ToTextOrDefault(unit, "/", "unit");
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName, WithFilters(_service.CreateFieldOutputEquation(nameText, equationText, unitText),
                                                 filter1Value, filter2Value));
        }

        [PythonAPI("Creates a coordinate system transform field output and adds it to the current result, " +
                   "in one call.",
                   Arguments = "name: str, field_name: str, coordinate_system_name: str, " +
                               "filter1: FieldOutputFilter = None, filter2: FieldOutputFilter = None",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI add_coordinate_system_transform(object name, object field_name,
                                                                    object coordinate_system_name,
                                                                    object filter1 = null,
                                                                    object filter2 = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string coordinateSystemName =
                PythonValueConverter.ToText(coordinate_system_name, "coordinate_system_name", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return add(WithFilters(_service.CreateFieldOutputCoordinateSystemTransform(nameText, fieldName,
                                                                                        coordinateSystemName),
                                   filter1Value, filter2Value));
        }

        [PythonAPI("Replaces a coordinate system transform field output, in one call.",
                   Arguments = "old_name: str, and the arguments of add_coordinate_system_transform",
                   Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace_coordinate_system_transform(
            object old_name, object name, object field_name, object coordinate_system_name,
            object filter1 = null, object filter2 = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string coordinateSystemName =
                PythonValueConverter.ToText(coordinate_system_name, "coordinate_system_name", false);
            ResultFieldOutputFilterAPI filter1Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter1, "filter1", true);
            ResultFieldOutputFilterAPI filter2Value =
                PythonValueConverter.ToApiObject<ResultFieldOutputFilterAPI>(filter2, "filter2", true);
            return replace(oldName,
                           WithFilters(_service.CreateFieldOutputCoordinateSystemTransform(
                                           nameText, fieldName, coordinateSystemName), filter1Value, filter2Value));
        }

        [PythonAPI("Validates and adds a field-output draft to the current result.",
                   Arguments = "output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI add(object output)
        {
            ResultFieldOutputDraftAPI outputValue =
                PythonValueConverter.ToApiObject<ResultFieldOutputDraftAPI>(output, "output", false);
            return _service.AddFieldOutput(_resultName, outputValue);
        }

        [PythonAPI("Validates and replaces a result field-output definition using command history. The draft " +
                   "must be of the type the replaced output is made by.",
                   Arguments = "old_name: str, output: FieldOutputDraft", Returns = "FieldOutput")]
        public ResultFieldOutputAPI replace(object old_name, object output)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            ResultFieldOutputDraftAPI outputValue =
                PythonValueConverter.ToApiObject<ResultFieldOutputDraftAPI>(output, "output", false);
            return _service.ReplaceFieldOutput(_resultName, oldName, outputValue);
        }

        [PythonAPI("Renames a result field output using command history.",
                   Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(object old_name, object new_name)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string newName = PythonValueConverter.ToText(new_name, "new_name", false);
            _service.RenameFieldOutput(_resultName, oldName, newName);
        }

        [PythonAPI("Duplicates result field outputs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            _service.DuplicateFieldOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes result field outputs using command history. A name is matched " +
                   "regardless of case.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            _service.RemoveFieldOutputs(_resultName, names);
        }

        [PythonAPI("Validates and removes components from a result field output using command history. The " +
                   "field output and component names are matched regardless of case.",
                   Arguments = "field_output_name: str, component_names: list[str]", Returns = "None")]
        public void remove_components(object field_output_name, object component_names)
        {
            string fieldOutputName = PythonValueConverter.ToText(field_output_name, "field_output_name", false);
            _service.RemoveFieldOutputComponents(_resultName, fieldOutputName, component_names);
        }

        [PythonAPI("Validates and exports all components of a result field output to CSV using command history.",
                   Arguments = "file_name: str, field_output_name: str, step_id: int, increment_id: int",
                   Returns = "None")]
        public void export_to_csv(object file_name, object field_output_name, object step_id, object increment_id)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string fieldOutputName = PythonValueConverter.ToText(field_output_name, "field_output_name", false);
            _service.ExportFieldOutputToCsv(_resultName, fileName, fieldOutputName,
                                            PythonValueConverter.ToInt(step_id, "step_id"),
                                            PythonValueConverter.ToInt(increment_id, "increment_id"));
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
                                                                object numberOfLayers, object numberOfIterations)
        {
            FieldResultFilterSourceTypeEnum sourceType =
                PythonValueConverter.ToEnum<FieldResultFilterSourceTypeEnum>(source, "source");
            int layers = PythonValueConverter.ToIntOrDefault(numberOfLayers, 1, "number_of_layers");
            int iterations = PythonValueConverter.ToIntOrDefault(numberOfIterations, 1, "number_of_iterations");
            if (layers < 1) throw new PythonAPIException("The argument 'number_of_layers' must be at least 1.");
            if (iterations < 1)
                throw new PythonAPIException("The argument 'number_of_iterations' must be at least 1.");
            return new ResultFieldOutputFilterAPI(type, sourceType, layers, iterations);
        }
    }
}
