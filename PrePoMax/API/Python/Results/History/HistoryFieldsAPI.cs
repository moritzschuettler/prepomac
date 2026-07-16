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
        private readonly string _historyOutputName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistoryFieldsAPI(string resultName, string historyOutputName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
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
                _historyService.ResolveField(_resultName, _historyOutputName, fieldName);
                return new HistoryFieldAPI(_resultName, _historyOutputName, fieldName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetFieldNames(_resultName, _historyOutputName).Length;
        }

        [PythonAPI("Returns history field names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetFieldNames(_resultName, _historyOutputName));
        }

        [PythonAPI("Returns the first field in this history output.", Returns = "HistoryField")]
        public HistoryFieldAPI first()
        {
            string[] names = _historyService.GetFieldNames(_resultName, _historyOutputName);
            if (names.Length == 0) throw new PythonAPIException("The history fields collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last field in this history output.", Returns = "HistoryField")]
        public HistoryFieldAPI last()
        {
            string[] names = _historyService.GetFieldNames(_resultName, _historyOutputName);
            if (names.Length == 0) throw new PythonAPIException("The history fields collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history field exists in this history output.", Arguments = "field_name: str", Returns = "bool")]
        public bool contains(string fieldName)
        {
            return _historyService.GetFieldNames(_resultName, _historyOutputName).Contains(fieldName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetFieldNames(_resultName, _historyOutputName);
            return "HistoryFields(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _historyService.GetFieldNames(_resultName, _historyOutputName);
            return "HistoryFields(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
