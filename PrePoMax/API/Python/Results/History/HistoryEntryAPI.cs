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
    public class HistoryEntryAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly string _entryName;
        private readonly HistoryDataService _historyService;


        // Properties
        [PythonAPI("The history entry name.", Returns = "str")]
        public string name { get { return _entryName; } }


        // Constructors
        internal HistoryEntryAPI(string resultName, string setName, string fieldName, string componentName,
                                       string entryName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _fieldName = fieldName;
            _componentName = componentName;
            _entryName = entryName;
            _historyService = historyService;
        }


        // Methods
        [PythonAPI("Returns time and values for this history entry.", Returns = "HistoryData")]
        public HistoryComponentDataAPI get_data()
        {
            return _historyService.GetEntryData(_resultName, _setName, _fieldName, _componentName, _entryName);
        }
        public override string ToString()
        {
            return "HistoryEntry(name='" + _entryName + "')";
        }
    }
}
