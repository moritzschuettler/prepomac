// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;
using PrePoMax.Python.Journal;
using Calculix = FileInOut.Output.Calculix;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSetCalculixKeywordEditorState : PreprocessCommand
    {
        // Variables
        private OrderedDictionary<int[], Calculix.CalculixUserKeyword> _userKeywords;
        private List<Calculix.CalculixKeywordOverride> _keywordOverrides;


        // Constructors
        public CSetCalculixKeywordEditorState(
            OrderedDictionary<int[], Calculix.CalculixUserKeyword> userKeywords,
            List<Calculix.CalculixKeywordOverride> keywordOverrides)
            : base("Set CalculiX keyword editor state")
        {
            _userKeywords = userKeywords.DeepClone();
            _keywordOverrides = keywordOverrides.DeepClone();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.SetCalculixKeywordEditorState(_userKeywords.DeepClone(), _keywordOverrides.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            List<string> pairs = new List<string>();
            foreach (var entry in _userKeywords)
            {
                pairs.Add("(" + PythonValueFormatter.List(entry.Key) + ", " +
                          PythonValueFormatter.Text(entry.Value.Data) + ")");
            }
            writer.Statement(writer.Call("pmx.model.keywords", "replace_user_keywords",
                                         "[" + string.Join(", ", pairs) + "]"));
            // Clearing refuses when there is nothing to clear, and the replayed model may have no overrides yet
            writer.Statement("if pmx.model.keywords.override_count > 0: " +
                             writer.Call("pmx.model.keywords", "clear_overrides"));
            foreach (Calculix.CalculixKeywordOverride keywordOverride in _keywordOverrides)
            {
                writer.Statement(writer.Call("pmx.model.keywords", "set_override",
                                             PythonValueFormatter.List(keywordOverride.Indices),
                                             "data=" + PythonValueFormatter.Text(keywordOverride.Data),
                                             "disabled=" +
                                             PythonValueFormatter.Format(keywordOverride.Disabled)));
            }
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _userKeywords.Count + ", " + _keywordOverrides.Count;
        }
    }
}
