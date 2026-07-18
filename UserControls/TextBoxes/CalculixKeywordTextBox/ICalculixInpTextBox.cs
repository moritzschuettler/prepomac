// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Windows.Forms;

namespace UserControls
{
    public interface ICalculixInpTextBox
    {
        // Events                                                                                                                   
        event EventHandler TextChanged;


        // Properties                                                                                                               
        Control Control { get; }
        string Text { get; set; }
        bool ReadOnly { get; set; }
        BorderStyle BorderStyle { get; set; }
        int LineCount { get; }


        // Methods                                                                                                                  
        void Clear();
        string GetActivatedText();
        string GetDeactivatedText();
        string GetLine(int index);
        string GetTextRows();
        void ApplyCalculixStyles();
        string ReplaceLines(int firstLine, int numOfLines, string newText, bool adding);
        void SelectLinesAndScrollToSelection(int firstLine, int numOfLines);
    }
}