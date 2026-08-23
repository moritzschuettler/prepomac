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
        [PythonAPI("The merged history source components as 'history_output_name.field_name.component_name'.",
                   Returns = "StringList")]
        public StringListAPI sources { get { return new StringListAPI(GetSourceNames()); } }


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
        private string[] GetSourceNames()
        {
            HistoryResultData[] sources = ResolveFromMergedHistories().GetSources();
            string[] sourceNames = new string[sources.Length];
            for (int i = 0; i < sources.Length; i++) sourceNames[i] = sources[i].GetDisplayName();
            return sourceNames;
        }
        public override string ToString()
        {
            return "HistoryOutputFromMergedHistories(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", sources=" + GetSourceNames().Length + ")";
        }
    }
}
