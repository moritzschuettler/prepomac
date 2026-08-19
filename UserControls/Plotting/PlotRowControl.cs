// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public class PlotRowControl : UserControl
    {
        private static readonly Color[] DefaultColors = new Color[]
        {
            Color.FromArgb(31, 119, 180), Color.FromArgb(214, 39, 40),
            Color.FromArgb(44, 160, 44), Color.FromArgb(255, 127, 14),
            Color.FromArgb(148, 103, 189), Color.FromArgb(23, 190, 207),
            Color.FromArgb(140, 86, 75), Color.FromArgb(227, 119, 194)
        };

        private readonly Panel _header;
        private readonly System.Windows.Forms.Label _title;
        private readonly Button _dataButton;
        private readonly Button _removeButton;
        private readonly ZedGraphControl _graph;
        private bool _active;

        public int PlotIndex { get; set; }
        public ZedGraphControl Graph { get { return _graph; } }

        public event EventHandler<PlotEventArgs> Activated;
        public event EventHandler<PlotEventArgs> DataRequested;
        public event EventHandler<PlotEventArgs> RemoveRequested;

        public PlotRowControl()
        {
            DoubleBuffered = true;
            Margin = Padding.Empty;
            Padding = new Padding(1);
            BackColor = SystemColors.ControlDark;

            _header = new Panel();
            _header.Dock = DockStyle.Top;
            _header.Height = 25;
            _header.Padding = new Padding(5, 1, 2, 1);
            _header.BackColor = SystemColors.Control;

            _title = new System.Windows.Forms.Label();
            _title.Dock = DockStyle.Fill;
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.AutoEllipsis = true;
            _title.Text = "Plot";

            _removeButton = CreateHeaderButton("X", 26);
            _removeButton.Dock = DockStyle.Right;
            _removeButton.Click += RemoveButton_Click;

            _dataButton = CreateHeaderButton("Data", 44);
            _dataButton.Dock = DockStyle.Right;
            _dataButton.Click += DataButton_Click;

            _header.Controls.Add(_title);
            _header.Controls.Add(_dataButton);
            _header.Controls.Add(_removeButton);

            _graph = new ZedGraphControl();
            _graph.Dock = DockStyle.Fill;
            _graph.IsShowPointValues = true;
            _graph.MouseDown += Child_MouseDown;
            _header.MouseDown += Child_MouseDown;
            _title.MouseDown += Child_MouseDown;

            Controls.Add(_graph);
            Controls.Add(_header);
            ConfigureGraphPane(_graph.GraphPane);
        }

        private Button CreateHeaderButton(string text, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Dock = DockStyle.Right;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Margin = Padding.Empty;
            button.TabStop = false;
            return button;
        }

        private void ConfigureGraphPane(GraphPane pane)
        {
            pane.IsFontsScaled = false;
            pane.IsPenWidthScaled = false;
            pane.Fill = new Fill(SystemColors.Control);
            pane.Chart.Fill = new Fill(Color.White);
            pane.Title.IsVisible = false;
            pane.Legend.Position = LegendPos.TopCenter;
            pane.Legend.FontSpec.Size = 10;
            pane.XAxis.Title.FontSpec.Size = 10;
            pane.XAxis.Scale.FontSpec.Size = 10;
            pane.YAxis.Title.FontSpec.Size = 10;
            pane.YAxis.Scale.FontSpec.Size = 10;
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.Color = Color.Gainsboro;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.Color = Color.Gainsboro;
            pane.XAxis.MinorGrid.IsVisible = false;
            pane.YAxis.MinorGrid.IsVisible = false;
        }

        public void SetData(PlotViewData data)
        {
            if (data == null) data = new PlotViewData();

            _title.Text = string.IsNullOrWhiteSpace(data.Title) ? "Plot" : data.Title;
            GraphPane pane = _graph.GraphPane;
            pane.CurveList.Clear();
            pane.XAxis.Title.Text = data.XAxisTitle ?? "";
            pane.YAxis.Title.Text = data.YAxisTitle ?? "";

            PlotCurveViewData[] curves = data.Curves ?? new PlotCurveViewData[0];
            for (int i = 0; i < curves.Length; i++)
            {
                PlotCurveViewData curve = curves[i];
                if (curve == null) continue;
                double[] x = curve.XValues ?? new double[0];
                double[] y = curve.YValues ?? new double[0];
                int count = Math.Min(x.Length, y.Length);
                double[] plotX = new double[count];
                double[] plotY = new double[count];
                Array.Copy(x, plotX, count);
                Array.Copy(y, plotY, count);
                Color color = curve.Color.IsEmpty ? DefaultColors[i % DefaultColors.Length] : curve.Color;
                LineItem item = pane.AddCurve(curve.Name ?? "Curve", plotX, plotY, color, SymbolType.None);
                item.Line.Width = 2;
                item.IsVisible = curve.Visible;
            }

            pane.Legend.IsVisible = curves.Length > 0;
            pane.AxisChange();
            _graph.Invalidate();
        }

        public void SetActive(bool active)
        {
            _active = active;
            BackColor = _active ? SystemColors.Highlight : SystemColors.ControlDark;
            _header.BackColor = _active ? SystemColors.GradientInactiveCaption : SystemColors.Control;
        }

        private void Child_MouseDown(object sender, MouseEventArgs e)
        {
            OnActivated();
        }

        private void DataButton_Click(object sender, EventArgs e)
        {
            OnActivated();
            EventHandler<PlotEventArgs> handler = DataRequested;
            if (handler != null) handler(this, new PlotEventArgs(PlotIndex));
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            EventHandler<PlotEventArgs> handler = RemoveRequested;
            if (handler != null) handler(this, new PlotEventArgs(PlotIndex));
        }

        private void OnActivated()
        {
            EventHandler<PlotEventArgs> handler = Activated;
            if (handler != null) handler(this, new PlotEventArgs(PlotIndex));
        }
    }
}
