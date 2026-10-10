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
    public class ResultHistoryOutputEntriesAPI : BaseAPI, IReadOnlyList<ResultHistoryOutputEntryAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;


        // Constructors
        internal ResultHistoryOutputEntriesAPI(string resultName, string historyOutputName, string fieldName, string componentName,
                                         ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _fieldName = fieldName;
            _componentName = componentName;
            _resultHistoryService = resultHistoryService;
        }


        // Methods
        public ResultHistoryOutputEntryAPI __getitem__(object entryName)
        {
            string entryNameText = PythonValueConverter.ToText(entryName, "name", false);
            return this[entryNameText];
        }
        public ResultHistoryOutputEntryAPI this[string entryName]
        {
            get
            {
                if (!contains(entryName))
                    throw new PythonAPIException("The history entry '" + entryName + "' does not exist in component '" +
                                                 _componentName + "'.");
                return new ResultHistoryOutputEntryAPI(_resultName, _historyOutputName, _fieldName, _componentName, entryName,
                                           _resultHistoryService);
            }
        }
        public int __len__()
        {
            return _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultHistoryOutputEntryAPI IReadOnlyList<ResultHistoryOutputEntryAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().ToArray();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<ResultHistoryOutputEntryAPI> GetEnumerator()
        {
            foreach (string key in get_names().ToArray()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns history entry names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName,
                                                                    _componentName));
        }

        [PythonAPI("Returns the first entry in this history component.", Returns = "HistoryEntry")]
        public ResultHistoryOutputEntryAPI first()
        {
            string[] names = _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName);
            if (names.Length == 0) throw new PythonAPIException("The history entries collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last entry in this history component.", Returns = "HistoryEntry")]
        public ResultHistoryOutputEntryAPI last()
        {
            string[] names = _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName);
            if (names.Length == 0) throw new PythonAPIException("The history entries collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history entry exists in this component.", Arguments = "name: str", Returns = "bool")]
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName,
                                                 _componentName).Contains(nameText);
        }
        public override string ToString()
        {
            string[] names = _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName);
            return "HistoryEntries(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName);
            return "HistoryEntries(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
