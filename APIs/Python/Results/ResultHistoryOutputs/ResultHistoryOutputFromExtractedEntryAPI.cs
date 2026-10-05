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
    public class ResultHistoryOutputFromExtractedEntryAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The source history component as [history_output_name, field_name, component_name], the " +
                   "form the source argument takes.", Returns = "StringList")]
        public StringListAPI source { get { return ToSourceNames(ResolveFromExtractedEntry().Source); } }
        [PythonAPI("The name of the extracted history entry.", Returns = "str")]
        public string entry_name { get { return ResolveFromExtractedEntry().EntryName; } }


        // Constructors
        internal ResultHistoryOutputFromExtractedEntryAPI(string resultName, string historyOutputName,
                                                          ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromExtractedEntry ResolveFromExtractedEntry()
        {
            ResultHistoryOutputFromExtractedEntry historyOutput = Resolve() as ResultHistoryOutputFromExtractedEntry;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a From Extracted Entry history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            ResultHistoryOutputFromExtractedEntry historyOutput = ResolveFromExtractedEntry();
            return "HistoryOutputFromExtractedEntry(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", source=" + StringListAPI.ToDisplayString(historyOutput.Source.GetDisplayName()) +
                   ", entry_name=" + StringListAPI.ToDisplayString(historyOutput.EntryName) + ")";
        }
    }
}
