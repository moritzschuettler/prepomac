// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace PrePoMax.API.Python
{
    public class ResultHistoryOutputAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;
        private readonly ResultHistoryOutputFieldsAPI _fields;


        // Properties
        [PythonAPI("The history output name.", Returns = "str")]
        public string name { get { return _historyOutputName; } }
        [PythonAPI("The fields in this history output.", Returns = "HistoryFields")]
        public ResultHistoryOutputFieldsAPI fields { get { return _fields; } }


        // Constructors
        internal ResultHistoryOutputAPI(string resultName, string historyOutputName, ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _resultHistoryService = resultHistoryService;
            _fields = new ResultHistoryOutputFieldsAPI(_resultName, _historyOutputName, _resultHistoryService);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryOutput(name='" + _historyOutputName + "', fields=" +
                   _resultHistoryService.GetFieldNames(_resultName, _historyOutputName).Length + ")";
        }
    }
}
