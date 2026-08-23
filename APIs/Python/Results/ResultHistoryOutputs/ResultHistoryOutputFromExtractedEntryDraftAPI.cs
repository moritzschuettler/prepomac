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
    public class ResultHistoryOutputFromExtractedEntryDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly HistoryResultData _source;
        private readonly string _entryName;


        // Properties
        [PythonAPI("The source history component as 'history_output_name.field_name.component_name'.", Returns = "str")]
        public string source { get { return _source.GetDisplayName(); } }
        [PythonAPI("The name of the extracted history entry.", Returns = "str")]
        public string entry_name { get { return _entryName; } }


        // Constructors
        internal ResultHistoryOutputFromExtractedEntryDraftAPI(string name, HistoryResultData source, string entryName)
            : base(name, null)
        {
            _source = source;
            _entryName = entryName;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            return new ResultHistoryOutputFromExtractedEntry(name, _source, _entryName);
        }
        protected override string GetRequiredTarget()
        {
            return null;
        }
        public override string ToString()
        {
            return "HistoryOutputFromExtractedEntryDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", source=" + StringListAPI.ToDisplayString(source) +
                   ", entry_name=" + StringListAPI.ToDisplayString(_entryName) + ")";
        }
    }
}
