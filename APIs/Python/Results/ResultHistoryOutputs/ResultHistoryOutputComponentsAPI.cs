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
    public class ResultHistoryOutputComponentsAPI : BaseAPI, IReadOnlyList<ResultHistoryOutputComponentAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly string _fieldName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;


        // Constructors
        internal ResultHistoryOutputComponentsAPI(string resultName, string historyOutputName, string fieldName,
                                            ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _fieldName = fieldName;
            _resultHistoryService = resultHistoryService;
        }


        // Methods
        public ResultHistoryOutputComponentAPI __getitem__(string componentName)
        {
            return this[componentName];
        }
        public ResultHistoryOutputComponentAPI this[string componentName]
        {
            get
            {
                _resultHistoryService.ResolveComponent(_resultName, _historyOutputName, _fieldName, componentName);
                return new ResultHistoryOutputComponentAPI(_resultName, _historyOutputName, _fieldName, componentName, _resultHistoryService);
            }
        }
        public int __len__()
        {
            return _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultHistoryOutputComponentAPI IReadOnlyList<ResultHistoryOutputComponentAPI>.this[int index]
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
        public IEnumerator<ResultHistoryOutputComponentAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns history component names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName));
        }

        [PythonAPI("Returns the first component in this history field.", Returns = "HistoryComponent")]
        public ResultHistoryOutputComponentAPI first()
        {
            string[] names = _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName);
            if (names.Length == 0) throw new PythonAPIException("The history components collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last component in this history field.", Returns = "HistoryComponent")]
        public ResultHistoryOutputComponentAPI last()
        {
            string[] names = _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName);
            if (names.Length == 0) throw new PythonAPIException("The history components collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history component exists in this field.", Arguments = "component_name: str", Returns = "bool")]
        public bool contains(string componentName)
        {
            return _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName).Contains(componentName);
        }
        public override string ToString()
        {
            string[] names = _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName);
            return "HistoryComponents(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName);
            return "HistoryComponents(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
