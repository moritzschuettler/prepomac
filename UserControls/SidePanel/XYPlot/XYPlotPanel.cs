// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public partial class XYPlotPanel : SidePanel
    {
        // Variables                                                                                                                
        private const int _maxPlotCount = 4;
        private static readonly Color _highlightColor = Color.FromArgb(214, 227, 245);
        private static readonly Color _highlightHeaderColor = Color.LightSteelBlue;
        private int _columnCount;
        private bool _sharedXValues;
        private bool _splitterDistanceInitialized;
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
            _sharedXValues = true;
            _splitterDistanceInitialized = false;
            _plots = new List<XYPlot>();
            _selectedPlot = null;
            //
            dgvData.EnableHeadersVisualStyles = false;
            InitializeToolTips();
            //
            splitContainer.SizeChanged += splitContainer_SizeChanged;
            dgvData.ColumnHeaderMouseClick += dgvData_ColumnHeaderMouseClick;
            dgvData.ColumnHeaderMouseDoubleClick += dgvData_ColumnHeaderMouseDoubleClick;
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
        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                XYPlot plot = _selectedPlot;
                if (plot == null && _plots.Count > 0) plot = _plots[0];
                //
                if (plot != null) plot.SaveAsImage();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnSigleColumn_Click(object sender, EventArgs e)
        {
            try
            {
                ColumnCount = 1;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnDoubleColumn_Click(object sender, EventArgs e)
        {
            try
            {
                ColumnCount = 2;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
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
        private void Plot_Changed(XYPlot plot)
        {
            if (plot == _selectedPlot) FillDataGrid(plot);
        }
        private void dgvData_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (_selectedPlot == null) return;
                //
                int curveId = GetCurveIdFromColumnId(e.ColumnIndex);
                CurveList curves = _selectedPlot.Pane.CurveList;
                //
                if (curveId >= 0 && curveId < curves.Count) _selectedPlot.EditCurve(curves[curveId]);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void Plot_CurveSelected(XYPlot plot)
        {
            if (plot == _selectedPlot) HighlightDataGridColumns(plot);
        }
        private void splitContainer_SizeChanged(object sender, EventArgs e)
        {
            InitializeSplitterDistance();
        }
        private void dgvData_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (_selectedPlot == null) return;
                //
                int curveId = GetCurveIdFromColumnId(e.ColumnIndex);
                CurveList curves = _selectedPlot.Pane.CurveList;
                //
                if (curveId >= 0 && curveId < curves.Count) _selectedPlot.SelectedCurve = curves[curveId];
                else
                {
                    _selectedPlot.SelectedCurve = null;
                    dgvData.ClearSelection();
                }
            }
            catch
            {
            }
        }


        // Methods                                                                                                                  
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            //
            InitializeSplitterDistance();
        }
        private void InitializeSplitterDistance()
        {
            if (_splitterDistanceInitialized || !Visible || !IsHandleCreated) return;
            //
            int distance = splitContainer.Height / 2;
            if (distance < splitContainer.Panel1MinSize ||
                distance > splitContainer.Height - splitContainer.Panel2MinSize - splitContainer.SplitterWidth) return;
            //
            splitContainer.SplitterDistance = distance;
            _splitterDistanceInitialized = true;
        }
        public XYPlot AddPlot()
        {
            if (_plots.Count >= _maxPlotCount) return null;
            //
            XYPlot plot = new XYPlot();
            plot.CloseButtonPressedEvent += Plot_CloseButtonPressed;
            plot.SelectedEvent += Plot_Selected;
            plot.PlotChangedEvent += Plot_Changed;
            plot.CurveSelectedEvent += Plot_CurveSelected;
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
            plot.PlotChangedEvent -= Plot_Changed;
            plot.CurveSelectedEvent -= Plot_CurveSelected;
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
            UpdateColumnButtons();
        }
        private void InitializeToolTips()
        {
            ttText.SetToolTip(btnSaveAs, "Save plot image as");
            ttText.SetToolTip(btnSigleColumn, "Arrange the plots in a single column");
            ttText.SetToolTip(btnDoubleColumn, "Arrange the plots in two columns");
            ttText.SetToolTip(btnClose, "Close XY plot panel");
        }
        private void UpdateColumnButtons()
        {
            btnSigleColumn.Enabled = _plots.Count > 1;
            btnDoubleColumn.Enabled = _plots.Count > 1;
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
            _sharedXValues = HaveEqualXValues(curves);
            //
            if (_sharedXValues)
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
            //
            dgvData.ClearSelection();
            //
            HighlightDataGridColumns(plot);
        }
        private void HighlightDataGridColumns(XYPlot plot)
        {
            if (plot == null) return;
            //
            int selectedCurveId = plot.SelectedCurve == null ? -1 : plot.Pane.CurveList.IndexOf(plot.SelectedCurve);
            //
            bool highlight;
            DataGridViewColumn column;
            for (int i = 0; i < dgvData.Columns.Count; i++)
            {
                column = dgvData.Columns[i];
                highlight = selectedCurveId >= 0 && GetCurveIdFromColumnId(i) == selectedCurveId;
                //
                column.DefaultCellStyle.BackColor = highlight ? _highlightColor : Color.Empty;
                column.HeaderCell.Style.BackColor = highlight ? _highlightHeaderColor : Color.Empty;
            }
        }
        private int GetCurveIdFromColumnId(int columnId)
        {
            if (columnId < 0) return -1;
            //
            if (_sharedXValues) return columnId - 1;
            else return columnId / 2;
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
            XYPlotCurveSettings settings = curve.Tag as XYPlotCurveSettings;
            if (settings != null && settings.Name.Length > 0) return settings.Name;
            //
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
