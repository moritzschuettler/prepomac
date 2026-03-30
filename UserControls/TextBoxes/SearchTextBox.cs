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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public partial class SearchTextBox : UserControl
    {
        // Variables                                                                                                                
        private Color textBoxBackColor;


        // Properties                                                                                                               
        [Browsable(true)]
        public override string Text { get { return tbSearchBox.Text; } set { tbSearchBox.Text = value; } }
        public bool ReadOnly { get { return tbSearchBox.ReadOnly; } set { tbSearchBox.ReadOnly = value; } }


        // Events                                                                                                                   
        [Browsable(true)]
        public new event Action<object, EventArgs> TextChanged;


        // Event handling                                                                                                           
        private void tbSearchBox_TextChanged(object sender, EventArgs e)
        {
            btnClear.Visible = tbSearchBox.Text.Length > 0;
            TextChanged?.Invoke(sender, e);
            Focus();
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            tbSearchBox.Text = "";
        }
        private void tbSearchBox_EnabledChanged(object sender, EventArgs e)
        {
            if (tbSearchBox.Enabled) tbSearchBox.BackColor = Color.Empty;
            else tbSearchBox.BackColor = textBoxBackColor;
        }


        // Constructors                                                                                                             
        public SearchTextBox()
        {
            InitializeComponent();
            //
            textBoxBackColor = Color.FromArgb(tbSearchBox.BackColor.ToArgb());  // copy only the color values
        }
    }
}
