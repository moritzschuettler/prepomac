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

namespace AutocompleteMenuNS
{
    [Serializable]
    public class Colors
    {
        public Color ForeColor { get; set; }
        public Color BackColor { get; set; }
        public Color SelectedForeColor { get; set; }
        public Color SelectedBackColor { get; set; }
        public Color SelectedBackColor2 { get; set; }
        public Color HighlightingColor { get; set; }

        public Colors()
        {
            ForeColor = Color.Black;
            BackColor = Color.White;
            SelectedForeColor = Color.White;
            SelectedBackColor = SystemColors.Highlight;
            SelectedBackColor2 = SystemColors.Highlight;
            HighlightingColor = SystemColors.Highlight;
        }
    }
}
