// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using FastColoredTextBoxNS;
using System;
using System.Drawing;
using System.Text.RegularExpressions;

namespace UserControls
{
    internal class PythonTextBoxSyntaxHighlighter : IDisposable
    {
        // Variables                                                                                                                
        private readonly TextStyle _keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly TextStyle _stringStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular);
        private readonly TextStyle _commentStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
        private readonly TextStyle _numberStyle = new TextStyle(Brushes.DarkCyan, null, FontStyle.Regular);


        // Methods                                                                                                                  
        public void Highlight(Range range)
        {
            range.ClearStyle(_keywordStyle, _stringStyle, _commentStyle, _numberStyle);

            // Comments
            range.SetStyle(_commentStyle, @"#.*$", RegexOptions.Multiline);

            // Strings
            range.SetStyle(_stringStyle, "\"\"\".*?\"\"\"", RegexOptions.Singleline);
            range.SetStyle(_stringStyle, "'''.*?'''", RegexOptions.Singleline);
            range.SetStyle(_stringStyle, "\"([^\"\\\\]|\\\\.)*\"");
            range.SetStyle(_stringStyle, "'([^'\\\\]|\\\\.)*'");

            // Numbers
            range.SetStyle(_numberStyle, @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b");

            // Python keywords
            range.SetStyle(_keywordStyle,
                @"\b(" +
                "False|None|True|" +
                "and|as|assert|async|await|break|class|continue|def|del|elif|else|" +
                "except|finally|for|from|global|if|import|in|is|lambda|nonlocal|" +
                "not|or|pass|raise|return|try|while|with|yield" +
                @")\b");
        }
        public void Dispose()
        {
            _keywordStyle.Dispose();
            _stringStyle.Dispose();
            _commentStyle.Dispose();
            _numberStyle.Dispose();
        }
    }
}
