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
    public class NoBeepMessageFilter : IMessageFilter
    {
        // Variables
        private const int WM_CHAR = 0x0102;


        // Methods
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_CHAR) return false;
            //
            char character = (char)m.WParam.ToInt32();
            if (character != '\r' && character != (char)Keys.Escape) return false;
            //
            Control control = Control.FromChildHandle(m.HWnd);
            if (control is TextBoxBase textBoxBase)
            {
                if (character != '\r' || !textBoxBase.Multiline) return true;
                else return control is TextBox textBox && !textBox.AcceptsReturn;
            }
            else if (control is ComboBox) return true;
            //
            return false;
        }
    }
}
