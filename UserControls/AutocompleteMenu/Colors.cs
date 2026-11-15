// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using CaeGlobals;

namespace AutocompleteMenuNS
{
    [Serializable]
    [PmxType(6200)]
    public class Colors
    {
        [field: PmxKey(0)] public Color ForeColor { get; set; }
        [field: PmxKey(1)] public Color BackColor { get; set; }
        [field: PmxKey(2)] public Color SelectedForeColor { get; set; }
        [field: PmxKey(3)] public Color SelectedBackColor { get; set; }
        [field: PmxKey(4)] public Color SelectedBackColor2 { get; set; }
        [field: PmxKey(5)] public Color HighlightingColor { get; set; }

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
