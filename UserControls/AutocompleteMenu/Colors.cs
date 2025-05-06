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
