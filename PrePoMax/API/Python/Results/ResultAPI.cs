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
        private readonly HistoryAPI _history;


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
        [PythonAPI("The result history outputs.", Returns = "History")]
        public HistoryAPI history { get { Resolve(); return _history; } }


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
            _history = new HistoryAPI(_resultName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "Result(name=" + StringListAPI.ToDisplayString(_resultName) + ", steps=" + _dataService.GetStepIds(_resultName).Length + ")";
        }
        private FeResults Resolve()
        {
            return _resolver.ResolveResult(_resultName);
        }
    }
}
