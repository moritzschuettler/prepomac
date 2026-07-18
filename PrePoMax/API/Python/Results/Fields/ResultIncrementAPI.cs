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

namespace PrePoMax.API.Python
{
    public class ResultIncrementAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly ResultDataService _dataService;
        private readonly FieldsAPI _fields;


        // Properties
        [PythonAPI("The result increment ID.", Returns = "int")]
        public int id { get { return _incrementId; } }
        [PythonAPI("The stored result time for this increment.", Returns = "float")]
        public float step_time { get { return _dataService.GetIncrementTime(_resultName, _stepId, _incrementId); } }
        [PythonAPI("Fields available for this increment.", Returns = "Fields")]
        public FieldsAPI fields { get { return _fields; } }


        // Constructors
        internal ResultIncrementAPI(string resultName, int stepId, int incrementId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _dataService = dataService;
            _fields = new FieldsAPI(_resultName, _stepId, _incrementId, _dataService);
        }


        // Methods
        public override string ToString()
        {
            return "Increment(id=" + _incrementId + ", step_time=" + step_time + ")";
        }
    }
}
