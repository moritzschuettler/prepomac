// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public partial class XYPlot : UserControl
    {
        // Variables                                                                                                                
        private const float _fontSize = 12;
        private static readonly Color[] _curveColors = new Color[]
        {
            Color.FromArgb(31, 119, 180), Color.FromArgb(214, 39, 40),
            Color.FromArgb(44, 160, 44), Color.FromArgb(255, 127, 14),
            Color.FromArgb(148, 103, 189), Color.FromArgb(23, 190, 207),
            Color.FromArgb(140, 86, 75), Color.FromArgb(227, 119, 194)
        };
        private const float _curveWidth = 2;
        private const float _selectedCurveWidth = 4;
        private const int _dimmedCurveAlpha = 60;
        private bool _closeButtonEnabled;
        private CurveItem _selectedCurve;
        private bool _highlighted;
        private Color _borderColor;
        private Color _highlightColor;


        // Properties                                                                                                               
        public ZedGraphControl Graph { get { return zedGraph; } }
        public GraphPane Pane { get { return zedGraph.GraphPane; } }
        public CurveItem SelectedCurve { get { return _selectedCurve; } set { SelectCurve(value); } }
        public string XAxisTitle
        {
            get { return zedGraph.GraphPane.XAxis.Title.Text; }
            set { zedGraph.GraphPane.XAxis.Title.Text = value; }
        }
        public string YAxisTitle
        {
            get { return zedGraph.GraphPane.YAxis.Title.Text; }
            set { zedGraph.GraphPane.YAxis.Title.Text = value; }
        }
        public bool Highlighted
        {
            get { return _highlighted; }
            set
            {
                if (value == _highlighted) return;
                _highlighted = value;
                Invalidate();
            }
        }
        public Color BorderColor { get { return _borderColor; } set { _borderColor = value; Invalidate(); } }
        public Color HighlightColor { get { return _highlightColor; } set { _highlightColor = value; Invalidate(); } }
        public bool CloseButtonEnabled
        {
            get { return _closeButtonEnabled; }
            set
            {
                _closeButtonEnabled = value;
                UpdateCloseButtonVisibility();
            }
        }


        // Events                                                                                                                   
        public event Action<XYPlot> CloseButtonPressedEvent;
        public event Action<XYPlot> SelectedEvent;
        public event Action<XYPlot> CurvesChangedEvent;


        // Constructors                                                                                                             
        public XYPlot()
        {
            DoubleBuffered = true;
            //
            InitializeComponent();
            //
            SetStyle(ControlStyles.ResizeRedraw, true);
            //
            _closeButtonEnabled = true;
            _selectedCurve = null;
            _highlighted = false;
            _borderColor = SystemColors.ControlDark;
            _highlightColor = SystemColors.Highlight;
            //
            InitializeGraphPane();
            //
            zedGraph.MouseDown += Child_MouseDown;
            panPlot.MouseDown += Child_MouseDown;
            zedGraph.ContextMenuBuilder += ZedGraph_ContextMenuBuilder;
            //
            MouseEnter += Child_MouseEnter;
            MouseLeave += Child_MouseLeave;
            panPlot.MouseEnter += Child_MouseEnter;
            panPlot.MouseLeave += Child_MouseLeave;
            zedGraph.MouseEnter += Child_MouseEnter;
            zedGraph.MouseLeave += Child_MouseLeave;
        }


        // Event handlers                                                                                                           
        private void Child_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                SelectedEvent?.Invoke(this);
                //
                if (sender == zedGraph)
                {
                    zedGraph.Focus();
                    SelectCurve(GetCurveAtPoint(e.Location));
                }
            }
            catch
            {
            }
        }
        private void Child_MouseEnter(object sender, EventArgs e)
        {
            UpdateCloseButtonVisibility();
        }
        private void Child_MouseLeave(object sender, EventArgs e)
        {
            UpdateCloseButtonVisibility();
        }
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
                UpdateCloseButtonVisibility();
            }
            catch
            {
            }
        }
        private void ZedGraph_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip,
                                                 Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            try
            {
                SelectedEvent?.Invoke(this);
                //
                if (zedGraph.GraphPane.CurveList.Count == 0) return;
                //
                int index = 0;
                CurveItem curve = GetCurveAtPoint(mousePt);
                if (curve != null)
                {
                    ToolStripMenuItem tsmiDeleteCurve = new ToolStripMenuItem("Delete Curve: " + curve.Label.Text);
                    tsmiDeleteCurve.Tag = curve;
                    tsmiDeleteCurve.Click += DeleteCurve_Click;
                    menuStrip.Items.Insert(index++, tsmiDeleteCurve);
                }
                ToolStripMenuItem tsmiDeleteAllCurves = new ToolStripMenuItem("Delete All Curves");
                tsmiDeleteAllCurves.Click += DeleteAllCurves_Click;
                menuStrip.Items.Insert(index++, tsmiDeleteAllCurves);
                menuStrip.Items.Insert(index++, new ToolStripSeparator());
            }
            catch
            {
            }
        }
        private void DeleteCurve_Click(object sender, EventArgs e)
        {
            try
            {
                RemoveCurve(((ToolStripMenuItem)sender).Tag as CurveItem);
            }
            catch
            {
            }
        }
        private void DeleteAllCurves_Click(object sender, EventArgs e)
        {
            try
            {
                RemoveAllCurves();
            }
            catch
            {
            }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseButtonPressedEvent?.Invoke(this);
            }
            catch
            {
            }
        }


        // Methods                                                                                                                  
        private void UpdateCloseButtonVisibility()
        {
            if (!IsHandleCreated) return;
            //
            bool visible = _closeButtonEnabled && ClientRectangle.Contains(PointToClient(MousePosition));
            if (visible == btnClose.Visible) return;
            //
            if (!visible) btnClose.Image = Properties.Resources.Close;
            btnClose.Visible = visible;
        }
        private void InitializeGraphPane()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            pane.IsFontsScaled = false;
            pane.IsPenWidthScaled = false;
            pane.Title.IsVisible = false;
            pane.Border.IsVisible = false;
            pane.Fill = new Fill(Color.White);
            pane.Chart.Fill = new Fill(Color.White);
            zedGraph.MasterPane.Fill = new Fill(Color.White);
            //
            pane.Legend.Position = LegendPos.TopCenter;
            pane.Legend.Border.IsVisible = false;
            pane.Legend.Fill = new Fill(Color.White);
            pane.Legend.FontSpec.Size = _fontSize;
            //
            pane.XAxis.Title.FontSpec.Size = _fontSize;
            pane.XAxis.Scale.FontSpec.Size = _fontSize;
            pane.YAxis.Title.FontSpec.Size = _fontSize;
            pane.YAxis.Scale.FontSpec.Size = _fontSize;
            //
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.Color = Color.Gainsboro;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.Color = Color.Gainsboro;
            pane.XAxis.MinorGrid.IsVisible = false;
            pane.YAxis.MinorGrid.IsVisible = false;
        }
        public void AddCurve(string curveName, double[] xValues, double[] yValues)
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            Color color = _curveColors[pane.CurveList.Count % _curveColors.Length];
            LineItem curve = pane.AddCurve(curveName, xValues, yValues, color, SymbolType.None);
            curve.Line.Width = _curveWidth;
            //
            UpdateCurveStyles();
            //
            pane.Legend.IsVisible = true;
            pane.AxisChange();
            //
            zedGraph.Invalidate();
        }
        private void SelectCurve(CurveItem curve)
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            if (curve != null && !pane.CurveList.Contains(curve)) return;
            if (curve == _selectedCurve) return;
            //
            _selectedCurve = curve;
            //
            UpdateCurveStyles();
            //
            zedGraph.Invalidate();
        }
        private void UpdateCurveStyles()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            bool selected;
            int alpha;
            foreach (CurveItem item in pane.CurveList)
            {
                if (!(item is LineItem lineItem)) continue;
                //
                selected = item == _selectedCurve;
                alpha = _selectedCurve == null || selected ? 255 : _dimmedCurveAlpha;
                //
                lineItem.Line.Width = selected ? _selectedCurveWidth : _curveWidth;
                lineItem.Line.Color = Color.FromArgb(alpha, lineItem.Line.Color);
                if (lineItem.Label.FontSpec == null) lineItem.Label.FontSpec = new FontSpec(pane.Legend.FontSpec);
                lineItem.Label.FontSpec.IsBold = selected;
            }
        }
        private CurveItem GetCurveAtPoint(Point mousePt)
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            using (Graphics g = zedGraph.CreateGraphics())
            {
                object nearestObject;
                int index;
                if (pane.FindNearestObject(mousePt, g, out nearestObject, out index))
                {
                    if (nearestObject is CurveItem curveItem) return curveItem;
                    if (nearestObject is Legend && index >= 0 && index < pane.CurveList.Count) return pane.CurveList[index];
                }
            }
            //
            CurveItem curve;
            int pointIndex;
            if (pane.FindNearestPoint(mousePt, out curve, out pointIndex)) return curve;
            //
            return null;
        }
        public void RemoveCurve(CurveItem curve)
        {
            if (curve == null) return;
            //
            GraphPane pane = zedGraph.GraphPane;
            if (!pane.CurveList.Contains(curve)) return;
            //
            pane.CurveList.Remove(curve);
            //
            UpdateAfterCurvesChanged();
        }
        public void RemoveAllCurves()
        {
            GraphPane pane = zedGraph.GraphPane;
            if (pane.CurveList.Count == 0) return;
            //
            pane.CurveList.Clear();
            //
            UpdateAfterCurvesChanged();
        }
        private void UpdateAfterCurvesChanged()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            if (_selectedCurve != null && !pane.CurveList.Contains(_selectedCurve)) _selectedCurve = null;
            //
            UpdateCurveStyles();
            //
            if (pane.CurveList.Count == 0)
            {
                pane.Legend.IsVisible = false;
                pane.XAxis.Title.Text = "";
                pane.YAxis.Title.Text = "";
            }
            pane.AxisChange();
            //
            zedGraph.Invalidate();
            //
            CurvesChangedEvent?.Invoke(this);
        }
        public void Clear()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            pane.CurveList.Clear();
            pane.GraphObjList.Clear();
            //
            UpdateAfterCurvesChanged();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete && _selectedCurve != null)
            {
                RemoveCurve(_selectedCurve);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            //
            using (Pen pen = new Pen(_highlighted ? _highlightColor : _borderColor, 2))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
            }
        }
    }
}
