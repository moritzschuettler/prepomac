// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;
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
        public override string GetCommandString()
        {
            return base.GetCommandString() + _userKeywords.Count + ", " + _keywordOverrides.Count;
        }
    }
}
