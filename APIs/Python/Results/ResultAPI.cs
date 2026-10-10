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
    public class ResultAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultDataService _dataService;
        private readonly MeshDataService _meshService;
        private readonly ResultHistoryOutputDataService _resultHistoryService;
        private readonly ResultFieldOutputService _resultFieldOutputService;
        private readonly ResultHistoryOutputService _resultHistoryOutputService;
        private readonly string _resultName;
        private readonly ResultStepsAPI _steps;
        private readonly MeshAPI _mesh;
        private readonly ResultHistoryOutputsAPI _historyOutputs;
        private readonly ResultFieldOutputsAPI _fieldOutputs;
        private readonly IPythonCommandGateway _commandGateway;


        // Properties
        [PythonAPI("The immutable user-visible result name.", Returns = "str")]
        public string name { get { return _resultName; } }
        [PythonAPI("The source result file name.", Returns = "str")]
        public string file_name { get { return Resolve().FileName; } }
        [PythonAPI("The result file date and time as an ISO-8601 string in local time, without fractions of a " +
                   "second or a time zone, for example '2026-10-10T14:03:22'.", Returns = "str")]
        public string date_time
        {
            get { return Resolve().DateTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture); }
        }
        [PythonAPI("The result unit system.", Returns = "str")]
        public string unit_system { get { return Resolve().UnitSystem != null ? Resolve().UnitSystem.ToString() : null; } }
        [PythonAPI("Result steps indexed by result step ID.", Returns = "Steps")]
        public ResultStepsAPI steps { get { Resolve(); return _steps; } }
        [PythonAPI("The result mesh.", Returns = "Mesh")]
        public MeshAPI mesh { get { Resolve(); return _mesh; } }
        [PythonAPI("Result history outputs indexed by history output name.", Returns = "HistoryOutputs")]
        public ResultHistoryOutputsAPI history_outputs { get { Resolve(); return _historyOutputs; } }
        [PythonAPI("User-defined result field outputs indexed by field output name.", Returns = "FieldOutputs")]
        public ResultFieldOutputsAPI field_outputs { get { Resolve(); return _fieldOutputs; } }


        // Constructors
        internal ResultAPI(ResultResolver resolver, string resultName, ResultFieldOutputService resultFieldOutputService,
                           ResultHistoryOutputService resultHistoryOutputService, SelectionService selectionService,
                           IPythonCommandGateway commandGateway)
        {
            _resolver = resolver;
            _resultName = resultName;
            _resultFieldOutputService = resultFieldOutputService;
            _resultHistoryOutputService = resultHistoryOutputService;
            _commandGateway = commandGateway;
            _dataService = new ResultDataService(resolver);
            _meshService = new MeshDataService(resolver);
            _resultHistoryService = new ResultHistoryOutputDataService(resolver);
            _steps = new ResultStepsAPI(_resultName, _dataService);
            _mesh = new MeshAPI(_resultName, _meshService, selectionService, _commandGateway, _resolver);
            _historyOutputs = new ResultHistoryOutputsAPI(_resultName, _resultHistoryService, _resultHistoryOutputService);
            _fieldOutputs = new ResultFieldOutputsAPI(_resultName, _resultFieldOutputService);
        }


        // Methods
        public override string ToString()
        {
            return "Result(name=" + StringListAPI.ToDisplayString(_resultName) + ", steps=" +
                   _dataService.GetStepIds(_resultName).Length + ")";
        }
        [PythonAPI("Returns one nodal field component value by node ID.",
                   Arguments = "step_id: int, increment_id: int, field_name: str, component_name: str, node_id: int",
                   Returns = "float")]
        public float get_nodal_value(object step_id, object increment_id, object field_name, object component_name,
                                     object node_id)
        {
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            return _dataService.GetNodalValue(_resultName, PythonValueConverter.ToInt(step_id, "step_id"),
                                              PythonValueConverter.ToInt(increment_id, "increment_id"), fieldName,
                                              componentName, PythonValueConverter.ToInt(node_id, "node_id"));
        }
        [PythonAPI("Returns the undeformed nodal coordinates [x, y, z] by node ID.",
                   Arguments = "node_id: int",
                   Returns = "list[float]")]
        public object get_nodal_coordinates(object node_id)
        {
            return _meshService.GetNodeCoordinates(_resultName, PythonValueConverter.ToInt(node_id, "node_id"));
        }
        [PythonAPI("Returns the true-scale deformed nodal coordinates [x, y, z] using the DISP field.",
                   Arguments = "step_id: int, increment_id: int, node_id: int",
                   Returns = "list[float]")]
        public object get_deformed_nodal_coordinates(object step_id, object increment_id, object node_id)
        {
            return _dataService.GetDeformedNodalCoordinates(_resultName, PythonValueConverter.ToInt(step_id, "step_id"),
                                                            PythonValueConverter.ToInt(increment_id, "increment_id"),
                                                            PythonValueConverter.ToInt(node_id, "node_id"));
        }
        [PythonAPI("Returns the entries in a history output field component.",
                   Arguments = "history_output_name: str, field_name: str, component_name: str",
                   Returns = "HistoryEntries")]
        public ResultHistoryOutputEntriesAPI get_history_output_entries(object history_output_name, object field_name,
                                                                        object component_name)
        {
            string historyOutputName = PythonValueConverter.ToText(history_output_name, "history_output_name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            _resultHistoryService.ResolveComponent(_resultName, historyOutputName, fieldName, componentName);
            return new ResultHistoryOutputEntriesAPI(_resultName, historyOutputName, fieldName, componentName,
                                                     _resultHistoryService);
        }
        [PythonAPI("Returns one named entry in a history output field component.",
                   Arguments = "history_output_name: str, field_name: str, component_name: str, entry_name: str",
                   Returns = "HistoryEntry")]
        public ResultHistoryOutputEntryAPI get_history_output_entry(object history_output_name, object field_name,
                                                                    object component_name, object entry_name)
        {
            string historyOutputName = PythonValueConverter.ToText(history_output_name, "history_output_name", false);
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            string entryName = PythonValueConverter.ToText(entry_name, "entry_name", false);
            _resultHistoryService.ResolveEntry(_resultName, historyOutputName, fieldName, componentName, entryName);
            return new ResultHistoryOutputEntryAPI(_resultName, historyOutputName, fieldName, componentName, entryName,
                                                   _resultHistoryService);
        }
        private FeResults Resolve()
        {
            return _resolver.ResolveResult(_resultName);
        }
    }
}
