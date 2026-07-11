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
    public class HistoryAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly HistoryDataService _historyService;
        private readonly HistorySetsAPI _sets;


        // Properties
        [PythonAPI("History result sets.", Returns = "HistorySets")]
        public HistorySetsAPI sets { get { return _sets; } }


        // Constructors
        internal HistoryAPI(string resultName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _historyService = historyService;
            _sets = new HistorySetsAPI(_resultName, _historyService);
        }


        // Methods
        public override string ToString()
        {
            return "History(sets=" + _historyService.GetSetNames(_resultName).Length + ")";
        }
    }
}
