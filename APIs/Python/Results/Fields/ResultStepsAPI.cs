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
    public class ResultStepsAPI : BaseAPI, IReadOnlyList<ResultStepAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly ResultDataService _dataService;


        // Constructors
        internal ResultStepsAPI(string resultName, ResultDataService dataService)
        {
            _resultName = resultName;
            _dataService = dataService;
        }


        // Methods
        public ResultStepAPI __getitem__(int stepId)
        {
            return this[stepId];
        }
        public ResultStepAPI this[int stepId]
        {
            get
            {
                if (!contains(stepId))
                    throw new PythonAPIException("The step ID " + stepId + " does not exist in result '" + _resultName + "'.");
                return new ResultStepAPI(_resultName, stepId, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetStepIds(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the ID indexer
        public int Count { get { return __len__(); } }
        ResultStepAPI IReadOnlyList<ResultStepAPI>.this[int index]
        {
            get
            {
                int[] keys = get_ids().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<ResultStepAPI> GetEnumerator()
        {
            foreach (int key in get_ids().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

        [PythonAPI("Returns the first result step.", Returns = "Step")]
        public ResultStepAPI first()
        {
            int[] ids = _dataService.GetStepIds(_resultName);
            if (ids.Length == 0) throw new PythonAPIException("The steps collection is empty.");
            return this[ids[0]];
        }

        [PythonAPI("Returns the last result step.", Returns = "Step")]
        public ResultStepAPI last()
        {
            int[] ids = _dataService.GetStepIds(_resultName);
            if (ids.Length == 0) throw new PythonAPIException("The steps collection is empty.");
            return this[ids[ids.Length - 1]];
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
