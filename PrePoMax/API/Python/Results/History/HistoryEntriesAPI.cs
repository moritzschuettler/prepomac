// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System.Linq;

namespace PrePoMax.API.Python
{
    public class HistoryEntriesAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistoryEntriesAPI(string resultName, string setName, string fieldName, string componentName,
                                         HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _fieldName = fieldName;
            _componentName = componentName;
            _historyService = historyService;
        }


        // Methods
        public HistoryEntryAPI __getitem__(string entryName)
        {
            return this[entryName];
        }
        public HistoryEntryAPI this[string entryName]
        {
            get
            {
                if (!contains(entryName))
                    throw new PythonAPIException("The history entry '" + entryName + "' does not exist in component '" +
                                                 _componentName + "'.");
                return new HistoryEntryAPI(_resultName, _setName, _fieldName, _componentName, entryName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName).Length;
        }

        [PythonAPI("Returns history entry names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName));
        }

        [PythonAPI("Returns true if the history entry exists in this component.", Arguments = "entry_name: str", Returns = "bool")]
        public bool contains(string entryName)
        {
            return _historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName).Contains(entryName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName);
            return "HistoryEntries(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
