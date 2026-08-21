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
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public partial class XYPlotPanel : SidePanel
    {
        // Variables                                                                                                                
        private const int _maxPlotCount = 4;
        private int _columnCount;
        private List<XYPlot> _plots;
        private XYPlot _selectedPlot;


        // Properties                                                                                                               
        public DataGridViewCopyPaste Data { get { return dgvData; } }
        public string Title { get { return labTitle.Text; } set { labTitle.Text = value; } }
        public int MaxPlotCount { get { return _maxPlotCount; } }
        public int PlotCount { get { return _plots.Count; } }
        public XYPlot[] Plots { get { return _plots.ToArray(); } }
        public int ColumnCount
        {
            get { return _columnCount; }
            set
            {
                int columnCount = value < 1 ? 1 : (value > 2 ? 2 : value);
                if (columnCount == _columnCount) return;
                _columnCount = columnCount;
                ArrangePlots();
            }
        }
        public XYPlot SelectedPlot { get { return _selectedPlot; } set { SelectPlot(value); } }


        // Events                                                                                                                   
        public event Action<object, EventArgs> CloseButtonPressedEvent;
        public event Action<XYPlot> PlotRemovedEvent;
        public event Action<XYPlot> SelectedPlotChangedEvent;


        // Constructors                                                                                                             
        public XYPlotPanel()
        {
            InitializeComponent();
            //
            _columnCount = 1;
            _plots = new List<XYPlot>();
            _selectedPlot = null;
            //
            AddPlot();
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
        private void Plot_CloseButtonPressed(XYPlot plot)
        {
            RemovePlot(plot);
        }
        private void Plot_Selected(XYPlot plot)
        {
            SelectPlot(plot);
        }
        private void Plot_CurvesChanged(XYPlot plot)
        {
            if (plot == _selectedPlot) FillDataGrid(plot);
        }


        // Methods                                                                                                                  
        public XYPlot AddPlot()
        {
            if (_plots.Count >= _maxPlotCount) return null;
            //
            XYPlot plot = new XYPlot();
            plot.CloseButtonPressedEvent += Plot_CloseButtonPressed;
            plot.SelectedEvent += Plot_Selected;
            plot.CurvesChangedEvent += Plot_CurvesChanged;
            //
            _plots.Add(plot);
            ArrangePlots();
            SelectPlot(plot);
            //
            return plot;
        }
        public void RemovePlot(XYPlot plot)
        {
            if (plot == null || !_plots.Remove(plot)) return;
            //
            plot.CloseButtonPressedEvent -= Plot_CloseButtonPressed;
            plot.SelectedEvent -= Plot_Selected;
            plot.CurvesChangedEvent -= Plot_CurvesChanged;
            //
            tlpPlots.Controls.Remove(plot);
            if (_selectedPlot == plot) _selectedPlot = null;
            //
            PlotRemovedEvent?.Invoke(plot);
            //
            plot.Dispose();
            ArrangePlots();
            //
            if (_plots.Count == 0) AddPlot();
            else if (_selectedPlot == null) SelectPlot(_plots[0]);
        }
        public void ResetPlots()
        {
            XYPlot[] plots = _plots.ToArray();
            for (int i = 0; i < plots.Length; i++) RemovePlot(plots[i]);
        }
        private void SelectPlot(XYPlot plot)
        {
            if (plot != null && !_plots.Contains(plot)) return;
            if (plot == _selectedPlot) return;
            //
            _selectedPlot = plot;
            for (int i = 0; i < _plots.Count; i++) _plots[i].Highlighted = _plots[i] == _selectedPlot;
            //
            FillDataGrid(_selectedPlot);
            //
            SelectedPlotChangedEvent?.Invoke(_selectedPlot);
        }
        private void ArrangePlots()
        {
            tlpPlots.SuspendLayout();
            //
            tlpPlots.Controls.Clear();
            //
            int plotCount = _plots.Count < 1 ? 1 : _plots.Count;
            int columnCount = _columnCount < plotCount ? _columnCount : plotCount;
            int rowCount = (plotCount + columnCount - 1) / columnCount;
            //
            tlpPlots.ColumnStyles.Clear();
            tlpPlots.RowStyles.Clear();
            tlpPlots.ColumnCount = columnCount;
            tlpPlots.RowCount = rowCount;
            for (int i = 0; i < columnCount; i++)
                tlpPlots.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
            for (int i = 0; i < rowCount; i++)
                tlpPlots.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            //
            for (int i = 0; i < _plots.Count; i++)
            {
                _plots[i].Dock = DockStyle.Fill;
                _plots[i].Margin = Padding.Empty;
                tlpPlots.Controls.Add(_plots[i], i % columnCount, i / columnCount);
            }
            //
            tlpPlots.ResumeLayout(true);
            //
            UpdateCloseButtons();
        }
        private void UpdateCloseButtons()
        {
            for (int i = 0; i < _plots.Count; i++) _plots[i].CloseButtonEnabled = _plots.Count > 1;
        }
        public void UpdateDataGrid()
        {
            FillDataGrid(_selectedPlot);
        }
        private void FillDataGrid(XYPlot plot)
        {
            dgvData.Rows.Clear();
            dgvData.Columns.Clear();
            //
            if (plot == null) return;
            //
            GraphPane pane = plot.Pane;
            CurveList curves = pane.CurveList;
            if (curves.Count == 0) return;
            //
            if (HaveEqualXValues(curves))
            {
                AddDataGridColumn(GetAxisTitle(pane.XAxis.Title.Text, "X"));
                for (int i = 0; i < curves.Count; i++) AddDataGridColumn(GetCurveName(curves[i], i));
                //
                int rowCount = 0;
                for (int i = 0; i < curves.Count; i++)
                    if (curves[i].Points.Count > rowCount) rowCount = curves[i].Points.Count;
                //
                DataGridViewRow[] rows = new DataGridViewRow[rowCount];
                for (int i = 0; i < rowCount; i++)
                {
                    object[] values = new object[curves.Count + 1];
                    for (int j = 0; j < curves.Count; j++)
                    {
                        if (i >= curves[j].Points.Count) continue;
                        if (j == 0) values[0] = curves[j].Points[i].X;
                        values[j + 1] = curves[j].Points[i].Y;
                    }
                    rows[i] = GetDataGridRow(values);
                }
                dgvData.Rows.AddRange(rows);
            }
            else
            {
                for (int i = 0; i < curves.Count; i++)
                {
                    string curveName = GetCurveName(curves[i], i);
                    AddDataGridColumn(curveName + " - " + GetAxisTitle(pane.XAxis.Title.Text, "X"));
                    AddDataGridColumn(curveName + " - " + GetAxisTitle(pane.YAxis.Title.Text, "Y"));
                }
                //
                int rowCount = 0;
                for (int i = 0; i < curves.Count; i++)
                    if (curves[i].Points.Count > rowCount) rowCount = curves[i].Points.Count;
                //
                DataGridViewRow[] rows = new DataGridViewRow[rowCount];
                for (int i = 0; i < rowCount; i++)
                {
                    object[] values = new object[2 * curves.Count];
                    for (int j = 0; j < curves.Count; j++)
                    {
                        if (i >= curves[j].Points.Count) continue;
                        values[2 * j] = curves[j].Points[i].X;
                        values[2 * j + 1] = curves[j].Points[i].Y;
                    }
                    rows[i] = GetDataGridRow(values);
                }
                dgvData.Rows.AddRange(rows);
            }
        }
        private void AddDataGridColumn(string headerText)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = "col" + dgvData.Columns.Count;
            column.HeaderText = headerText;
            column.ReadOnly = true;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.DefaultCellStyle.Format = "G12";
            dgvData.Columns.Add(column);
        }
        private DataGridViewRow GetDataGridRow(object[] values)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dgvData, values);
            return row;
        }
        private bool HaveEqualXValues(CurveList curves)
        {
            for (int i = 1; i < curves.Count; i++)
            {
                if (curves[i].Points.Count != curves[0].Points.Count) return false;
                for (int j = 0; j < curves[i].Points.Count; j++)
                    if (curves[i].Points[j].X != curves[0].Points[j].X) return false;
            }
            return true;
        }
        private string GetCurveName(CurveItem curve, int curveId)
        {
            if (curve.Label == null || string.IsNullOrEmpty(curve.Label.Text)) return "Curve " + (curveId + 1);
            return curve.Label.Text;
        }
        private string GetAxisTitle(string axisTitle, string defaultTitle)
        {
            if (string.IsNullOrEmpty(axisTitle)) return defaultTitle;
            return axisTitle;
        }
        public override void UpdateDesign()
        {
            for (int i = 0; i < _plots.Count; i++) _plots[i].Graph.Invalidate();
        }
    }
}
