// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeGlobals
{
    [Serializable]
    public class MessageBoxPrompt
    {
        // Variables
        private readonly string _caption;
        private readonly string _text;
        private readonly string _buttons;
        private readonly string _answer;
        private readonly bool _isDecision;
        private readonly bool _isRefused;


        // Properties
        public string Caption { get { return _caption; } }
        public string Text { get { return _text; } }
        public string Buttons { get { return _buttons; } }
        public string Answer { get { return _answer; } }
        // A decision prompt offers more than one answer; a single button box is only a notification
        public bool IsDecision { get { return _isDecision; } }
        public bool IsRefused { get { return _isRefused; } }


        // Constructor
        public MessageBoxPrompt(string caption, string text, string buttons, string answer, bool isDecision, bool isRefused)
        {
            _caption = caption;
            _text = text;
            _buttons = buttons;
            _answer = answer;
            _isDecision = isDecision;
            _isRefused = isRefused;
        }


        // Methods
        public override string ToString()
        {
            return _caption + ": " + _text;
        }
    }
}
