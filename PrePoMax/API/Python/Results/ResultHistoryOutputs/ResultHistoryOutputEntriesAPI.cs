// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace PrePoMax.API.Python
{
    public class ResultHistoryOutputEntriesAPI : BaseAPI
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
        public ResultHistoryOutputEntryAPI __getitem__(string entryName)
        {
            return this[entryName];
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

        [PythonAPI("Returns true if the history entry exists in this component.", Arguments = "entry_name: str", Returns = "bool")]
        public bool contains(string entryName)
        {
            return _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName,
                                                 _componentName).Contains(entryName);
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
