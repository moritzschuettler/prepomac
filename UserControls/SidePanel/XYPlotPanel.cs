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
    public partial class PlotPanel : SidePanel
    {
        // Variables


        // Properties
        public ZedGraph.ZedGraphControl Plot { get { return zedGraph; } }
        public DataGridViewCopyPaste Data { get { return dgvData; } }
        public string Title { get { return labTitle.Text; } set { labTitle.Text = value; } }


        // Events
        public event Action<object, EventArgs> CloseButtonPressedEvent;


        // Constructors
        public PlotPanel()
        {
            InitializeComponent();
        }


        // Event handlers
        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                btnClose.Image = Properties.Resources.Close_White;
            }
            catch
            {
            }
        }
        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                btnClose.Image = Properties.Resources.Close;
            }
            catch
            {
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseButtonPressedEvent?.Invoke(sender, e);
            }
            catch
            {
            }
        }


        // Methods
        public override void UpdateDesign()
        {
            zedGraph.Invalidate();
        }
    }
}
