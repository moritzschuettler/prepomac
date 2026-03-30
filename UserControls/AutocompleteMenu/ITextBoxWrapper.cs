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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AutocompleteMenuNS
{
    /// <summary>
    /// Wrapper over the control like TextBox.
    /// </summary>
    public interface ITextBoxWrapper
    {
        Control TargetControl { get; }
        string Text { get; }
        string SelectedText { get; set; }
        int SelectionLength { get; set; }
        int SelectionStart { get; set; }
        Point GetPositionFromCharIndex(int pos);
        bool Readonly { get; }
        event EventHandler LostFocus;
        event ScrollEventHandler Scroll;
        event KeyEventHandler KeyDown;
        event MouseEventHandler MouseDown;
    }
}
