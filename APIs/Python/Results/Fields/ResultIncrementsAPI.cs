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
    public class ResultIncrementsAPI : BaseAPI, IReadOnlyList<ResultIncrementAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly ResultDataService _dataService;


        // Constructors
        internal ResultIncrementsAPI(string resultName, int stepId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _dataService = dataService;
        }


        // Methods
        public ResultIncrementAPI __getitem__(int incrementId)
        {
            return this[incrementId];
        }
        public ResultIncrementAPI this[int incrementId]
        {
            get
            {
                if (!contains(incrementId))
                    throw new PythonAPIException("The increment ID " + incrementId + " does not exist in step " + _stepId +
                                                 " of result '" + _resultName + "'.");
                return new ResultIncrementAPI(_resultName, _stepId, incrementId, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetIncrementIds(_resultName, _stepId).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the ID indexer
        public int Count { get { return __len__(); } }
        ResultIncrementAPI IReadOnlyList<ResultIncrementAPI>.this[int index]
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
        public IEnumerator<ResultIncrementAPI> GetEnumerator()
        {
            foreach (int key in get_ids().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(int incrementId)
        {
            return contains(incrementId);
        }

        [PythonAPI("Returns all result increment IDs for this step.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return new IntListAPI(_dataService.GetIncrementIds(_resultName, _stepId));
        }

        [PythonAPI("Returns the first result increment in this step.", Returns = "Increment")]
        public ResultIncrementAPI first()
        {
            int[] ids = _dataService.GetIncrementIds(_resultName, _stepId);
            if (ids.Length == 0) throw new PythonAPIException("The increments collection is empty.");
            return this[ids[0]];
        }

        [PythonAPI("Returns the last result increment in this step.", Returns = "Increment")]
        public ResultIncrementAPI last()
        {
            int[] ids = _dataService.GetIncrementIds(_resultName, _stepId);
            if (ids.Length == 0) throw new PythonAPIException("The increments collection is empty.");
            return this[ids[ids.Length - 1]];
        }

        [PythonAPI("Returns true if the increment ID exists in this step.", Arguments = "id: int", Returns = "bool")]
        public bool contains(int id)
        {
            return _dataService.GetIncrementIds(_resultName, _stepId).Contains(id);
        }
        public override string ToString()
        {
            return "Increments(count=" + _dataService.GetIncrementIds(_resultName, _stepId).Length + ")";
        }
    }
}
