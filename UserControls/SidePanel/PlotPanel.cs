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
using System.Windows.Forms;

namespace UserControls
{
    public class PlotPanel : SidePanel
    {
        // Variables                                                                                                                
        private readonly Panel _header;
        private readonly Panel _content;
        private readonly Label _title;
        private readonly Button _closeButton;
        private readonly PlotStackControl _plotStack;
        private readonly Label _emptyLabel;


        // Properties                                                                                                               
        public int ActivePlotIndex { get { return _plotStack.ActivePlotIndex; } }
        public int PlotCount { get { return _plotStack.PlotCount; } }


        // Events                                                                                                                   
        public event EventHandler CloseButtonPressedEvent;
        public event EventHandler<PlotEventArgs> ActivePlotChanged;
        public event EventHandler<PlotEventArgs> DataRequested;
        public event EventHandler<PlotEventArgs> RemoveRequested;


        // Constructors                                                                                                             
        public PlotPanel()
        {
            DoubleBuffered = true;
            AutoScroll = false;
            BackColor = SystemColors.Control;

            _header = new Panel();
            _header.Dock = DockStyle.Top;
            _header.Height = 31;
            _header.Padding = new Padding(7, 3, 3, 3);

            _title = new Label();
            _title.Dock = DockStyle.Fill;
            _title.Text = "Plots";
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.Font = new Font(_title.Font, FontStyle.Bold);

            _closeButton = new Button();
            _closeButton.Dock = DockStyle.Right;
            _closeButton.Width = 28;
            _closeButton.Text = "X";
            _closeButton.FlatStyle = FlatStyle.Flat;
            _closeButton.FlatAppearance.BorderSize = 0;
            _closeButton.TabStop = false;
            _closeButton.Click += CloseButton_Click;

            _header.Controls.Add(_title);
            _header.Controls.Add(_closeButton);

            _content = new Panel();
            _content.Dock = DockStyle.Fill;

            _plotStack = new PlotStackControl();
            _plotStack.Dock = DockStyle.Fill;
            _plotStack.ActivePlotChanged += PlotStack_ActivePlotChanged;
            _plotStack.DataRequested += PlotStack_DataRequested;
            _plotStack.RemoveRequested += PlotStack_RemoveRequested;

            _emptyLabel = new Label();
            _emptyLabel.Dock = DockStyle.Fill;
            _emptyLabel.Text = "Select a history output in the Results tree and use\r\nAdd to Plot or Add to New Plot.";
            _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            _emptyLabel.ForeColor = SystemColors.GrayText;

            _content.Controls.Add(_plotStack);
            _content.Controls.Add(_emptyLabel);
            Controls.Add(_content);
            Controls.Add(_header);
            _emptyLabel.BringToFront();
        }


        // Methods                                                                                                                  
        public void SetPlots(IList<PlotViewData> plots)
        {
            _plotStack.SetPlots(plots);
            _emptyLabel.Visible = plots == null || plots.Count == 0;
            if (!_emptyLabel.Visible) _plotStack.BringToFront();
        }
        public override void UpdateDesign()
        {
            _plotStack.Invalidate(true);
        }


        // Event handlers                                                                                                           
        private void CloseButton_Click(object sender, EventArgs e)
        {
            EventHandler handler = CloseButtonPressedEvent;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        private void PlotStack_ActivePlotChanged(object sender, PlotEventArgs e)
        {
            EventHandler<PlotEventArgs> handler = ActivePlotChanged;
            if (handler != null) handler(this, e);
        }
        private void PlotStack_DataRequested(object sender, PlotEventArgs e)
        {
            EventHandler<PlotEventArgs> handler = DataRequested;
            if (handler != null) handler(this, e);
        }
        private void PlotStack_RemoveRequested(object sender, PlotEventArgs e)
        {
            EventHandler<PlotEventArgs> handler = RemoveRequested;
            if (handler != null) handler(this, e);
        }
    }
}
