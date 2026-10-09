// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromMergedHistoriesAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The merged history source components, each as [history_output_name, field_name, " +
                   "component_name], the form the source1 to source4 arguments take.", Returns = "list[StringList]")]
        public object sources { get { return ToSourceNames(ResolveFromMergedHistories().GetSources()); } }


        // Constructors
        internal ResultHistoryOutputFromMergedHistoriesAPI(string resultName, string historyOutputName,
                                                           ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromMergedHistories ResolveFromMergedHistories()
        {
            ResultHistoryOutputFromMergedHistories historyOutput =
                Resolve() as ResultHistoryOutputFromMergedHistories;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a From Merged Histories history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            return "HistoryOutputFromMergedHistories(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", sources=" + ResolveFromMergedHistories().GetSources().Length + ")";
        }
    }
}
