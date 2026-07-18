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
    public class ResultHistoryOutputFieldAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly string _fieldName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;
        private readonly ResultHistoryOutputComponentsAPI _components;


        // Properties
        [PythonAPI("The history field name.", Returns = "str")]
        public string name { get { return _fieldName; } }
        [PythonAPI("The components in this history field.", Returns = "HistoryComponents")]
        public ResultHistoryOutputComponentsAPI components { get { return _components; } }


        // Constructors
        internal ResultHistoryOutputFieldAPI(string resultName, string historyOutputName, string fieldName,
                                       ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _fieldName = fieldName;
            _resultHistoryService = resultHistoryService;
            _components = new ResultHistoryOutputComponentsAPI(_resultName, _historyOutputName, _fieldName, _resultHistoryService);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryField(name='" + _fieldName + "', components=" +
                   _resultHistoryService.GetComponentNames(_resultName, _historyOutputName, _fieldName).Length + ")";
        }
    }
}
