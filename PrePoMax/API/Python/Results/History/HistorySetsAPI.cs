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
    public class HistorySetsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistorySetsAPI(string resultName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _historyService = historyService;
        }


        // Methods
        public HistorySetAPI __getitem__(string setName)
        {
            return this[setName];
        }
        public HistorySetAPI this[string setName]
        {
            get
            {
                _historyService.ResolveSet(_resultName, setName);
                return new HistorySetAPI(_resultName, setName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetSetNames(_resultName).Length;
        }

        [PythonAPI("Returns history set names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetSetNames(_resultName));
        }

        [PythonAPI("Returns true if the history set exists.", Arguments = "set_name: str", Returns = "bool")]
        public bool contains(string setName)
        {
            return _historyService.GetSetNames(_resultName).Contains(setName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetSetNames(_resultName);
            return "HistorySets(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
