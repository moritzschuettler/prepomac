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
        [PythonAPI("The result file date and time.", Returns = "DateTime")]
        public System.DateTime date_time { get { return Resolve().DateTime; } }
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
        public float get_nodal_value(int step_id, int increment_id, string field_name, string component_name, int node_id)
        {
            return _dataService.GetNodalValue(_resultName, step_id, increment_id, field_name, component_name, node_id);
        }
        [PythonAPI("Returns the undeformed nodal coordinates [x, y, z] by node ID.",
                   Arguments = "node_id: int",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_nodal_coordinates(int node_id)
        {
            return _meshService.GetNodeCoordinates(_resultName, node_id);
        }
        [PythonAPI("Returns the true-scale deformed nodal coordinates [x, y, z] using the DISP field.",
                   Arguments = "step_id: int, increment_id: int, node_id: int",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_deformed_nodal_coordinates(int step_id, int increment_id, int node_id)
        {
            return _dataService.GetDeformedNodalCoordinates(_resultName, step_id, increment_id, node_id);
        }
        [PythonAPI("Returns the entries in a history output field component.",
                   Arguments = "history_name: str, field_name: str, component_name: str",
                   Returns = "HistoryEntries")]
        public ResultHistoryOutputEntriesAPI get_history_output_entries(string history_name, string field_name, string component_name)
        {
            _resultHistoryService.ResolveComponent(_resultName, history_name, field_name, component_name);
            return new ResultHistoryOutputEntriesAPI(_resultName, history_name, field_name, component_name, _resultHistoryService);
        }
        [PythonAPI("Returns one named entry in a history output field component.",
                   Arguments = "history_name: str, field_name: str, component_name: str, entry_name: str",
                   Returns = "HistoryEntry")]
        public ResultHistoryOutputEntryAPI get_history_output_entry(string history_name, string field_name, string component_name,
                                                        string entry_name)
        {
            _resultHistoryService.ResolveEntry(_resultName, history_name, field_name, component_name, entry_name);
            return new ResultHistoryOutputEntryAPI(_resultName, history_name, field_name, component_name, entry_name,
                                                   _resultHistoryService);
        }
        private FeResults Resolve()
        {
            return _resolver.ResolveResult(_resultName);
        }
    }
}
