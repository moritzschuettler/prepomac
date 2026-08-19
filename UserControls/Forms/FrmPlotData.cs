// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Windows.Forms;

namespace UserControls
{
    public class FrmPlotData : Form
    {
        private readonly DataGridView _dataGridView;
        private readonly Button _copyButton;
        private readonly Button _exportButton;
        private readonly Button _closeButton;
        private PlotViewData _data;

        public int PlotIndex { get; private set; }

        public event EventHandler<PlotEventArgs> ExportRequested;

        public FrmPlotData()
        {
            Text = "Plot Data";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(600, 360);
            Size = new System.Drawing.Size(850, 560);
            ShowInTaskbar = false;

            _dataGridView = new DataGridView();
            _dataGridView.Dock = DockStyle.Fill;
            _dataGridView.ReadOnly = true;
            _dataGridView.AllowUserToAddRows = false;
            _dataGridView.AllowUserToDeleteRows = false;
            _dataGridView.AllowUserToOrderColumns = true;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            _dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            _dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.Height = 40;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(5);

            _closeButton = new Button();
            _closeButton.Text = "Close";
            _closeButton.AutoSize = true;
            _closeButton.Click += (sender, e) => Close();

            _exportButton = new Button();
            _exportButton.Text = "Export CSV...";
            _exportButton.AutoSize = true;
            _exportButton.Click += ExportButton_Click;

            _copyButton = new Button();
            _copyButton.Text = "Copy All";
            _copyButton.AutoSize = true;
            _copyButton.Click += CopyButton_Click;

            buttons.Controls.Add(_closeButton);
            buttons.Controls.Add(_exportButton);
            buttons.Controls.Add(_copyButton);
            Controls.Add(_dataGridView);
            Controls.Add(buttons);
        }

        public void SetData(int plotIndex, PlotViewData data)
        {
            PlotIndex = plotIndex;
            _data = data ?? new PlotViewData();
            Text = "Plot Data - " + (_data.Title ?? "Plot");
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            _dataGridView.Columns.Clear();
            _dataGridView.Rows.Clear();
            _dataGridView.Columns.Add("Curve", "Curve");
            _dataGridView.Columns.Add("Point", "Point");
            _dataGridView.Columns.Add("X", string.IsNullOrEmpty(_data.XAxisTitle) ? "X" : _data.XAxisTitle);
            _dataGridView.Columns.Add("Y", string.IsNullOrEmpty(_data.YAxisTitle) ? "Y" : _data.YAxisTitle);
            _dataGridView.Columns["Point"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dataGridView.Columns["X"].DefaultCellStyle.Format = "G12";
            _dataGridView.Columns["Y"].DefaultCellStyle.Format = "G12";

            PlotCurveViewData[] curves = _data.Curves ?? new PlotCurveViewData[0];
            foreach (PlotCurveViewData curve in curves)
            {
                if (curve == null) continue;
                double[] x = curve.XValues ?? new double[0];
                double[] y = curve.YValues ?? new double[0];
                int count = Math.Min(x.Length, y.Length);
                for (int i = 0; i < count; i++)
                    _dataGridView.Rows.Add(curve.Name, i + 1, x[i], y[i]);
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(PlotDataExporter.GetDelimitedText(_data, "\t"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Copy Plot Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            EventHandler<PlotEventArgs> handler = ExportRequested;
            if (handler != null) handler(this, new PlotEventArgs(PlotIndex));
        }
    }
}
