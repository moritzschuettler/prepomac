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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


namespace UserControls
{
    public class BorderColoredPanel : Panel
    {
        private Color _borderColor = Color.Gray;


        public Color BorderColor { get { return _borderColor; } set { _borderColor = value; } }


        public BorderColoredPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw
                     | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(BackColor)) e.Graphics.FillRectangle(brush, ClientRectangle);
            using (Pen borderPen = new Pen(_borderColor))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }
    }
}
