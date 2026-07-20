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
    public class ResultHistoryOutputFieldsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _historyOutputName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;


        // Constructors
        internal ResultHistoryOutputFieldsAPI(string resultName, string historyOutputName, ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _resultHistoryService = resultHistoryService;
        }


        // Methods
        public ResultHistoryOutputFieldAPI __getitem__(string fieldName)
        {
            return this[fieldName];
        }
        public ResultHistoryOutputFieldAPI this[string fieldName]
        {
            get
            {
                _resultHistoryService.ResolveField(_resultName, _historyOutputName, fieldName);
                return new ResultHistoryOutputFieldAPI(_resultName, _historyOutputName, fieldName, _resultHistoryService);
            }
        }
        public int __len__()
        {
            return _resultHistoryService.GetFieldNames(_resultName, _historyOutputName).Length;
        }

        [PythonAPI("Returns history field names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resultHistoryService.GetFieldNames(_resultName, _historyOutputName));
        }

        [PythonAPI("Returns the first field in this history output.", Returns = "HistoryField")]
        public ResultHistoryOutputFieldAPI first()
        {
            string[] names = _resultHistoryService.GetFieldNames(_resultName, _historyOutputName);
            if (names.Length == 0) throw new PythonAPIException("The history fields collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last field in this history output.", Returns = "HistoryField")]
        public ResultHistoryOutputFieldAPI last()
        {
            string[] names = _resultHistoryService.GetFieldNames(_resultName, _historyOutputName);
            if (names.Length == 0) throw new PythonAPIException("The history fields collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history field exists in this history output.", Arguments = "field_name: str", Returns = "bool")]
        public bool contains(string fieldName)
        {
            return _resultHistoryService.GetFieldNames(_resultName, _historyOutputName).Contains(fieldName);
        }
        public override string ToString()
        {
            string[] names = _resultHistoryService.GetFieldNames(_resultName, _historyOutputName);
            return "HistoryFields(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _resultHistoryService.GetFieldNames(_resultName, _historyOutputName);
            return "HistoryFields(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
