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
    public class StepAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly ResultDataService _dataService;
        private readonly IncrementsAPI _increments;


        // Properties
        [PythonAPI("The result step ID.", Returns = "int")]
        public int id { get { return _stepId; } }
        [PythonAPI("Result increments indexed by increment ID.", Returns = "Increments")]
        public IncrementsAPI increments { get { return _increments; } }


        // Constructors
        internal StepAPI(string resultName, int stepId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _dataService = dataService;
            _increments = new IncrementsAPI(_resultName, _stepId, _dataService);
        }


        // Methods
        public override string ToString()
        {
            return "Step(id=" + _stepId + ", increments=" + _dataService.GetIncrementIds(_resultName, _stepId).Length + ")";
        }
    }
}
