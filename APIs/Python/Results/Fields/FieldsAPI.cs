// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class FieldsAPI : BaseAPI, IReadOnlyList<FieldAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly ResultDataService _dataService;


        // Constructors
        internal FieldsAPI(string resultName, int stepId, int incrementId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _dataService = dataService;
        }


        // Methods
        public FieldAPI __getitem__(string fieldName)
        {
            return this[fieldName];
        }
        public FieldAPI this[string fieldName]
        {
            get
            {
                if (!contains(fieldName))
                    throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + _stepId +
                                                 ", increment " + _incrementId + ".");
                return new FieldAPI(_resultName, _stepId, _incrementId, fieldName, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetFieldNames(_resultName, _stepId, _incrementId).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        FieldAPI IReadOnlyList<FieldAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<FieldAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns field names available for this increment.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_dataService.GetFieldNames(_resultName, _stepId, _incrementId));
        }

        [PythonAPI("Returns the first field available for this increment.", Returns = "Field")]
        public FieldAPI first()
        {
            string[] names = _dataService.GetFieldNames(_resultName, _stepId, _incrementId);
            if (names.Length == 0) throw new PythonAPIException("The fields collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last field available for this increment.", Returns = "Field")]
        public FieldAPI last()
        {
            string[] names = _dataService.GetFieldNames(_resultName, _stepId, _incrementId);
            if (names.Length == 0) throw new PythonAPIException("The fields collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if this increment contains the specified field.", Arguments = "field_name: str", Returns = "bool")]
        public bool contains(string fieldName)
        {
            return _dataService.GetFieldNames(_resultName, _stepId, _incrementId).Contains(fieldName);
        }
        public override string ToString()
        {
            string[] names = _dataService.GetFieldNames(_resultName, _stepId, _incrementId);
            return "Fields(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _dataService.GetFieldNames(_resultName, _stepId, _incrementId);
            return "Fields(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
