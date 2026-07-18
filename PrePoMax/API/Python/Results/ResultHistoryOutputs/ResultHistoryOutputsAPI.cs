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
    public class ResultHistoryOutputsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;


        // Constructors
        internal ResultHistoryOutputsAPI(string resultName, ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _resultHistoryService = resultHistoryService;
        }


        // Methods
        public ResultHistoryOutputAPI __getitem__(string historyOutputName)
        {
            return this[historyOutputName];
        }
        public ResultHistoryOutputAPI this[string historyOutputName]
        {
            get
            {
                _resultHistoryService.ResolveHistoryOutput(_resultName, historyOutputName);
                return new ResultHistoryOutputAPI(_resultName, historyOutputName, _resultHistoryService);
            }
        }
        public int __len__()
        {
            return _resultHistoryService.GetHistoryOutputNames(_resultName).Length;
        }
        public bool __contains__(string historyOutputName)
        {
            return contains(historyOutputName);
        }

        [PythonAPI("Returns history output names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resultHistoryService.GetHistoryOutputNames(_resultName));
        }

        [PythonAPI("Returns the first history output.", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI first()
        {
            string[] names = _resultHistoryService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last history output.", Returns = "HistoryOutput")]
        public ResultHistoryOutputAPI last()
        {
            string[] names = _resultHistoryService.GetHistoryOutputNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The history outputs collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the history output exists.", Arguments = "history_output_name: str", Returns = "bool")]
        public bool contains(string historyOutputName)
        {
            return _resultHistoryService.GetHistoryOutputNames(_resultName).Contains(historyOutputName);
        }
        public override string ToString()
        {
            string[] names = _resultHistoryService.GetHistoryOutputNames(_resultName);
            return Format(names, false);
        }
        protected override string ToFullString()
        {
            string[] names = _resultHistoryService.GetHistoryOutputNames(_resultName);
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
