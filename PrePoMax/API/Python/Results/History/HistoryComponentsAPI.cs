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
    public class HistoryComponentsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _setName;
        private readonly string _fieldName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistoryComponentsAPI(string resultName, string setName, string fieldName,
                                            HistoryDataService historyService)
        {
            _resultName = resultName;
            _setName = setName;
            _fieldName = fieldName;
            _historyService = historyService;
        }


        // Methods
        public HistoryComponentAPI __getitem__(string componentName)
        {
            return this[componentName];
        }
        public HistoryComponentAPI this[string componentName]
        {
            get
            {
                _historyService.ResolveComponent(_resultName, _setName, _fieldName, componentName);
                return new HistoryComponentAPI(_resultName, _setName, _fieldName, componentName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetComponentNames(_resultName, _setName, _fieldName).Length;
        }

        [PythonAPI("Returns history component names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetComponentNames(_resultName, _setName, _fieldName));
        }

        [PythonAPI("Returns true if the history component exists in this field.", Arguments = "component_name: str", Returns = "bool")]
        public bool contains(string componentName)
        {
            return _historyService.GetComponentNames(_resultName, _setName, _fieldName).Contains(componentName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetComponentNames(_resultName, _setName, _fieldName);
            return "HistoryComponents(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
