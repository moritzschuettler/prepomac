// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace APIs.Python
{
    public class ResultHistoryOutputComponentAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;
        private readonly ResultHistoryOutputEntriesAPI _entries;


        // Properties
        [PythonAPI("The history component name.", Returns = "str")]
        public string name { get { return _componentName; } }
        [PythonAPI("The named entries in this history component. Integration point entries use elementId_intPointId names.", Returns = "HistoryEntries")]
        public ResultHistoryOutputEntriesAPI entries { get { return _entries; } }
        //
        internal string HistoryOutputName { get { return _historyOutputName; } }
        internal string FieldName { get { return _fieldName; } }
        internal string ComponentName { get { return _componentName; } }


        // Constructors
        internal ResultHistoryOutputComponentAPI(string resultName, string historyOutputName, string fieldName, string componentName,
                                           ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _fieldName = fieldName;
            _componentName = componentName;
            _resultHistoryService = resultHistoryService;
            _entries = new ResultHistoryOutputEntriesAPI(_resultName, _historyOutputName, _fieldName, _componentName, _resultHistoryService);
        }


        // Methods
        [PythonAPI("Returns all compatible history entries for this component.", Returns = "HistoryData")]
        public ResultHistoryOutputComponentDataAPI get_data()
        {
            return _resultHistoryService.GetComponentData(_resultName, _historyOutputName, _fieldName, _componentName);
        }

        [PythonAPI("Returns entry names for this component.", Returns = "StringList")]
        public StringListAPI get_entry_names()
        {
            return new StringListAPI(_resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName));
        }
        public override string ToString()
        {
            return "HistoryComponent(name='" + _componentName + "', entries=" +
                   _resultHistoryService.GetEntryNames(_resultName, _historyOutputName, _fieldName, _componentName).Length + ")";
        }
    }
}
