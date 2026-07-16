// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

namespace PrePoMax.API.Python
{
    public class HistoryOutputAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly HistoryDataService _historyService;
        private readonly HistoryFieldsAPI _fields;


        // Properties
        [PythonAPI("The history output name.", Returns = "str")]
        public string name { get { return _historyOutputName; } }
        [PythonAPI("The fields in this history output.", Returns = "HistoryFields")]
        public HistoryFieldsAPI fields { get { return _fields; } }


        // Constructors
        internal HistoryOutputAPI(string resultName, string historyOutputName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _historyService = historyService;
            _fields = new HistoryFieldsAPI(_resultName, _historyOutputName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryOutput(name='" + _historyOutputName + "', fields=" +
                   _historyService.GetFieldNames(_resultName, _historyOutputName).Length + ")";
        }
    }
}
