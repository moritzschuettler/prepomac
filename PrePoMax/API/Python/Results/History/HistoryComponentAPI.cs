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
    public class HistoryComponentAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly HistoryDataService _historyService;
        private readonly HistoryEntriesAPI _entries;


        // Properties
        [PythonAPI("The history component name.", Returns = "str")]
        public string name { get { return _componentName; } }
        [PythonAPI("The named entries in this history component. Integration point entries use elementId_intPointId names.", Returns = "HistoryEntries")]
        public HistoryEntriesAPI entries { get { return _entries; } }


        // Constructors
        internal HistoryComponentAPI(string resultName, string setName, string fieldName, string componentName,
                                           HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _fieldName = fieldName;
            _componentName = componentName;
            _historyService = historyService;
            _entries = new HistoryEntriesAPI(_resultName, _setName, _fieldName, _componentName, _historyService);
        }


        // Methods
        [PythonAPI("Returns all compatible history entries for this component.", Returns = "HistoryData")]
        public HistoryComponentDataAPI get_data()
        {
            return _historyService.GetComponentData(_resultName, _setName, _fieldName, _componentName);
        }

        [PythonAPI("Returns entry names for this component.", Returns = "StringList")]
        public StringListAPI get_entry_names()
        {
            return new StringListAPI(_historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName));
        }
        public override string ToString()
        {
            return "HistoryComponent(name='" + _componentName + "', entries=" +
                   _historyService.GetEntryNames(_resultName, _setName, _fieldName, _componentName).Length + ")";
        }
    }
}
