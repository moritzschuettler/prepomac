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
    public class ResultHistoryOutputsAPI : BaseAPI
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
                return new ResultHistoryOutputAPI(_resultName, historyOutputName, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetHistoryOutputNames(_resultName).Length;
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

        [PythonAPI("Returns true if the history output exists.", Arguments = "history_output_name: str", Returns = "bool")]
        public bool contains(string historyOutputName)
        {
            return _dataService.GetHistoryOutputNames(_resultName).Contains(historyOutputName);
        }

        [PythonAPI("Creates a From Field result history-output draft. The region can be a result node selection, " +
                   "a result node set, or a result surface. This does not modify the result.",
                   Arguments = "name: str, field_name: str, component_names: list[str], " +
                               "region: Selection or NodeSet or Surface",
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

        [PythonAPI("Creates a From Element Size draft. Possible size_type values are 'area' and 'volume'. " +
                   "Area accepts a surface-target selection or result surface; volume accepts an element-target " +
                   "selection or result element set. " +
                   "Possible deformation_variable_name values are 'Displacements', 'Forces', 'Surface Normals', " +
                   "'Wear Depths', 'Mesh Deformation', 'Disp&Def', and 'Disp&Def&Depth'.",
                   Arguments = "name: str, size_type: str, region: Selection or ElementSet or Surface, " +
                               "deformation_variable_name: str = 'Displacements'",
                   Returns = "HistoryOutputFromElementSizeDraft")]
        public ResultHistoryOutputFromElementSizeDraftAPI create_history_output_from_element_size(
            string name, string size_type, object region,
            string deformation_variable_name = "Displacements")
        {
            return _service.CreateFromElementSize(name, size_type, region, deformation_variable_name);
        }

        [PythonAPI("Creates a From Integration draft. Possible integral_type values are 'surface' and 'volume'. " +
                   "Surface integration accepts a surface-target selection or result surface; volume integration " +
                   "accepts an element-target selection or result element set. Possible deformation_variable_name " +
                   "values are 'Displacements', " +
                   "'Forces', 'Surface Normals', 'Wear Depths', 'Mesh Deformation', 'Disp&Def', and " +
                   "'Disp&Def&Depth'.",
                   Arguments = "name: str, integral_type: str, field_name: str, component_names: list[str], " +
                               "region: Selection or ElementSet or Surface, " +
                               "deformation_variable_name: str = 'Displacements', " +
                               "unit: str = '/'",
                   Returns = "HistoryOutputFromIntegrationDraft")]
        public ResultHistoryOutputFromIntegrationDraftAPI create_history_output_from_integration(
            string name, string integral_type, string field_name, object component_names,
            object region, string deformation_variable_name = "Displacements", string unit = "/")
        {
            return _service.CreateFromIntegration(name, integral_type, field_name, component_names, region,
                                                  deformation_variable_name, unit);
        }

        [PythonAPI("Creates a minimum history-output filter. Possible return_value values are 'row' and 'column'.",
                   Arguments = "return_value: str = 'column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_minimum_filter(string return_value = "column")
        {
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Minimum, return_value);
        }

        [PythonAPI("Creates a maximum history-output filter. Possible return_value values are 'row' and 'column'.",
                   Arguments = "return_value: str = 'column'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_maximum_filter(string return_value = "column")
        {
            return CreateMinMaxFilter(HistoryResultFilterTypeEnum.Maximum, return_value);
        }

        [PythonAPI("Creates a sum history-output filter. Possible operate_on values are 'rows' and 'columns'.",
                   Arguments = "operate_on: str = 'columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_sum_filter(string operate_on = "columns")
        {
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Sum, operate_on);
        }

        [PythonAPI("Creates an average history-output filter. Possible operate_on values are 'rows' and 'columns'.",
                   Arguments = "operate_on: str = 'columns'", Returns = "HistoryOutputFilter")]
        public ResultHistoryOutputFilterAPI create_average_filter(string operate_on = "columns")
        {
            return CreateAggregateFilter(HistoryResultFilterTypeEnum.Average, operate_on);
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

        [PythonAPI("Validates and removes result history outputs using command history.",
                   Arguments = "history_output_names: list[str]", Returns = "None")]
        public void remove(object history_output_names)
        {
            _service.RemoveHistoryOutputs(_resultName, history_output_names);
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
            string option;
            if (returnValue == "row") option = HistoryResultFilter.Row;
            else if (returnValue == "column") option = HistoryResultFilter.Column;
            else throw new PythonAPIException("The argument 'return_value' must be 'row' or 'column'.");
            return new ResultHistoryOutputFilterAPI(type, option);
        }
        private static ResultHistoryOutputFilterAPI CreateAggregateFilter(HistoryResultFilterTypeEnum type,
                                                                           string operateOn)
        {
            string option;
            if (operateOn == "rows") option = HistoryResultFilter.Rows;
            else if (operateOn == "columns") option = HistoryResultFilter.Columns;
            else throw new PythonAPIException("The argument 'operate_on' must be 'rows' or 'columns'.");
            return new ResultHistoryOutputFilterAPI(type, option);
        }
    }
}
