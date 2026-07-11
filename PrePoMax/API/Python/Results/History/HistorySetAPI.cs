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
    public class HistorySetAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly HistoryDataService _historyService;
        private readonly HistoryFieldsAPI _fields;


        // Properties
        [PythonAPI("The history set name.", Returns = "str")]
        public string name { get { return _setName; } }
        [PythonAPI("The fields in this history set.", Returns = "HistoryFields")]
        public HistoryFieldsAPI fields { get { return _fields; } }


        // Constructors
        internal HistorySetAPI(string resultName, string setName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _historyService = historyService;
            _fields = new HistoryFieldsAPI(_resultName, _setName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "HistorySet(name='" + _setName + "', fields=" +
                   _historyService.GetFieldNames(_resultName, _setName).Length + ")";
        }
    }
}
