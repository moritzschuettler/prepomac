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
    public class HistoryOutputsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly HistoryDataService _historyService;


        // Constructors
        internal HistoryOutputsAPI(string resultName, HistoryDataService historyService)
        {
            _resultName = resultName;
            _historyService = historyService;
        }


        // Methods
        public HistoryOutputAPI __getitem__(string historyOutputName)
        {
            return this[historyOutputName];
        }
        public HistoryOutputAPI this[string historyOutputName]
        {
            get
            {
                _historyService.ResolveHistoryOutput(_resultName, historyOutputName);
                return new HistoryOutputAPI(_resultName, historyOutputName, _historyService);
            }
        }
        public int __len__()
        {
            return _historyService.GetHistoryOutputNames(_resultName).Length;
        }
        public bool __contains__(string historyOutputName)
        {
            return contains(historyOutputName);
        }

        [PythonAPI("Returns history output names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_historyService.GetHistoryOutputNames(_resultName));
        }

        [PythonAPI("Returns the first history output.", Returns = "HistoryOutput")]
        public HistoryOutputAPI first()
        {
            string[] names = _historyService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last history output.", Returns = "HistoryOutput")]
        public HistoryOutputAPI last()
        {
            string[] names = _historyService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history output exists.", Arguments = "history_output_name: str", Returns = "bool")]
        public bool contains(string historyOutputName)
        {
            return _historyService.GetHistoryOutputNames(_resultName).Contains(historyOutputName);
        }
        public override string ToString()
        {
            string[] names = _historyService.GetHistoryOutputNames(_resultName);
            return Format(names, false);
        }
        protected override string ToFullString()
        {
            string[] names = _historyService.GetHistoryOutputNames(_resultName);
            return Format(names, true);
        }
        private static string Format(string[] names, bool full)
        {
            StringListAPI list = new StringListAPI(names);
            return "HistoryOutputs(count=" + names.Length + ", names=" +
                   (full ? list.GetFullDisplayString() : list.ToString()) + ")";
        }
    }
}
