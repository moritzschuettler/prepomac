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
    public class StepsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly ResultDataService _dataService;


        // Constructors
        internal StepsAPI(string resultName, ResultDataService dataService)
        {
            _resultName = resultName;
            _dataService = dataService;
        }


        // Methods
        public StepAPI __getitem__(int stepId)
        {
            return this[stepId];
        }
        public StepAPI this[int stepId]
        {
            get
            {
                if (!contains(stepId))
                    throw new PythonAPIException("The step ID " + stepId + " does not exist in result '" + _resultName + "'.");
                return new StepAPI(_resultName, stepId, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetStepIds(_resultName).Length;
        }
        public bool __contains__(int stepId)
        {
            return contains(stepId);
        }

        [PythonAPI("Returns all result step IDs.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return new IntListAPI(_dataService.GetStepIds(_resultName));
        }

        [PythonAPI("Returns true if the step ID exists.", Arguments = "step_id: int", Returns = "bool")]
        public bool contains(int stepId)
        {
            return _dataService.GetStepIds(_resultName).Contains(stepId);
        }
        public override string ToString()
        {
            return "Steps(count=" + _dataService.GetStepIds(_resultName).Length + ")";
        }
    }
}
