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
    public class HistoryFieldsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistoryFieldsAPI(string resultName, string setName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _historyService = historyService;
        }


        // Methods
        public HistoryFieldAPI __getitem__(string fieldName)
        {
            return this[fieldName];
        }
        public HistoryFieldAPI this[string fieldName]
        {
            get
            {
                _historyService.ResolveField(_resultName, _setName, fieldName);
                return new HistoryFieldAPI(_resultName, _setName, fieldName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetFieldNames(_resultName, _setName).Length;
        }

        [PythonAPI("Returns history field names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetFieldNames(_resultName, _setName));
        }

        [PythonAPI("Returns true if the history field exists in this set.", Arguments = "field_name: str", Returns = "bool")]
        public bool contains(string fieldName)
        {
            return _historyService.GetFieldNames(_resultName, _setName).Contains(fieldName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetFieldNames(_resultName, _setName);
            return "HistoryFields(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
