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
    public class HistoryFieldAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly string _fieldName;
        private readonly HistoryDataService _historyService;
        private readonly HistoryComponentsAPI _components;


        // Properties
        [PythonAPI("The history field name.", Returns = "str")]
        public string name { get { return _fieldName; } }
        [PythonAPI("The components in this history field.", Returns = "HistoryComponents")]
        public HistoryComponentsAPI components { get { return _components; } }


        // Constructors
        internal HistoryFieldAPI(string resultName, string setName, string fieldName,
                                       HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _fieldName = fieldName;
            _historyService = historyService;
            _components = new HistoryComponentsAPI(_resultName, _setName, _fieldName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryField(name='" + _fieldName + "', components=" +
                   _historyService.GetComponentNames(_resultName, _setName, _fieldName).Length + ")";
        }
    }
}
