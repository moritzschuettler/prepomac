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
    public class ResultStepAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly ResultDataService _dataService;
        private readonly ResultIncrementsAPI _increments;


        // Properties
        [PythonAPI("The result step ID.", Returns = "int")]
        public int id { get { return _stepId; } }
        [PythonAPI("Result increments indexed by increment ID.", Returns = "Increments")]
        public ResultIncrementsAPI increments { get { return _increments; } }


        // Constructors
        internal ResultStepAPI(string resultName, int stepId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _dataService = dataService;
            _increments = new ResultIncrementsAPI(_resultName, _stepId, _dataService);
        }


        // Methods
        public override string ToString()
        {
            return "Step(id=" + _stepId + ", increments=" + _dataService.GetIncrementIds(_resultName, _stepId).Length + ")";
        }
    }
}
