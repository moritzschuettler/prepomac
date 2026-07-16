// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeResults;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class ResultAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultDataService _dataService;
        private readonly MeshDataService _meshService;
        private readonly HistoryDataService _historyService;
        private readonly string _resultName;
        private readonly StepsAPI _steps;
        private readonly MeshAPI _mesh;
        private readonly HistoryOutputsAPI _historyOutputs;


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
        public StepsAPI steps { get { Resolve(); return _steps; } }
        [PythonAPI("The result mesh.", Returns = "Mesh")]
        public MeshAPI mesh { get { Resolve(); return _mesh; } }
        [PythonAPI("Result history outputs indexed by history output name.", Returns = "HistoryOutputs")]
        public HistoryOutputsAPI history_outputs { get { Resolve(); return _historyOutputs; } }


        // Constructors
        internal ResultAPI(ResultResolver resolver, string resultName)
        {
            _resolver = resolver;
            _resultName = resultName;
            _dataService = new ResultDataService(resolver);
            _meshService = new MeshDataService(resolver);
            _historyService = new HistoryDataService(resolver);
            _steps = new StepsAPI(_resultName, _dataService);
            _mesh = new MeshAPI(_resultName, _meshService);
            _historyOutputs = new HistoryOutputsAPI(_resultName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "Result(name=" + StringListAPI.ToDisplayString(_resultName) + ", steps=" + _dataService.GetStepIds(_resultName).Length + ")";
        }
        [PythonAPI("Returns one nodal field component value by node ID.",
                   Arguments = "step_id: int, increment_id: int, field_name: str, component_name: str, node_id: int",
                   Returns = "float")]
        public float get_nodal_value(int stepId, int incrementId, string fieldName, string componentName, int nodeId)
        {
            return _dataService.GetNodalValue(_resultName, stepId, incrementId, fieldName, componentName, nodeId);
        }
        [PythonAPI("Returns the undeformed nodal coordinates [x, y, z] by node ID.",
                   Arguments = "node_id: int",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_nodal_coordinates(int nodeId)
        {
            return _meshService.GetNodeCoordinates(_resultName, nodeId);
        }
        [PythonAPI("Returns the true-scale deformed nodal coordinates [x, y, z] using the DISP field.",
                   Arguments = "step_id: int, increment_id: int, node_id: int",
                   Returns = "numpy.ndarray or NumericArray")]
        public object get_deformed_nodal_coordinates(int stepId, int incrementId, int nodeId)
        {
            return _dataService.GetDeformedNodalCoordinates(_resultName, stepId, incrementId, nodeId);
        }
        [PythonAPI("Returns the entries in a history output field component.",
                   Arguments = "history_name: str, field_name: str, component_name: str",
                   Returns = "HistoryEntries")]
        public HistoryEntriesAPI get_history_output_entries(string historyName, string fieldName, string componentName)
        {
            _historyService.ResolveComponent(_resultName, historyName, fieldName, componentName);
            return new HistoryEntriesAPI(_resultName, historyName, fieldName, componentName, _historyService);
        }
        [PythonAPI("Returns one named entry in a history output field component.",
                   Arguments = "history_name: str, field_name: str, component_name: str, entry_name: str",
                   Returns = "HistoryEntry")]
        public HistoryEntryAPI get_history_output_entry(string historyName, string fieldName, string componentName,
                                                        string entryName)
        {
            _historyService.ResolveEntry(_resultName, historyName, fieldName, componentName, entryName);
            return new HistoryEntryAPI(_resultName, historyName, fieldName, componentName, entryName, _historyService);
        }
        private FeResults Resolve()
        {
            return _resolver.ResolveResult(_resultName);
        }
    }
}
