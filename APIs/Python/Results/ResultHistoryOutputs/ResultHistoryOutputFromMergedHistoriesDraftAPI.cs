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
    public class ResultHistoryOutputFromMergedHistoriesDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly HistoryResultData[] _sources;


        // Properties
        [PythonAPI("The merged history source components as 'history_output_name.field_name.component_name'.",
                   Returns = "StringList")]
        public StringListAPI sources { get { return new StringListAPI(GetSourceNames()); } }


        // Constructors
        internal ResultHistoryOutputFromMergedHistoriesDraftAPI(string name, HistoryResultData[] sources)
            : base(name, null)
        {
            _sources = sources;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            return new ResultHistoryOutputFromMergedHistories(name, _sources[0], _sources[1],
                                                              _sources.Length > 2 ? _sources[2] : null,
                                                              _sources.Length > 3 ? _sources[3] : null);
        }
        protected override string GetRequiredTarget()
        {
            return null;
        }
        private string[] GetSourceNames()
        {
            string[] sourceNames = new string[_sources.Length];
            for (int i = 0; i < _sources.Length; i++) sourceNames[i] = _sources[i].GetDisplayName();
            return sourceNames;
        }
        public override string ToString()
        {
            return "HistoryOutputFromMergedHistoriesDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", sources=" + _sources.Length + ")";
        }
    }
}
