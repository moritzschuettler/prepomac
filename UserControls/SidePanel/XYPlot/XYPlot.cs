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
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public partial class XYPlot : UserControl
    {
        // Variables                                                                                                                

        private static readonly Color[] _curveColors = new Color[]
        {
            Color.FromArgb(31, 119, 180), Color.FromArgb(214, 39, 40),
            Color.FromArgb(44, 160, 44), Color.FromArgb(255, 127, 14),
            Color.FromArgb(148, 103, 189), Color.FromArgb(23, 190, 207),
            Color.FromArgb(140, 86, 75), Color.FromArgb(227, 119, 194)
        };
        private const float _curveWidth = 2;
        private const float _selectedCurveWidthIncrement = 2;
        private const int _dimmedCurveAlpha = 60;
        private const double _curveHitTolerance = 8;
        private static readonly string[] _hiddenMenuItemTags = new string[]
        {
            "save_as", "page_setup", "print"
        };
        private const bool _defaultShowPointSymbols = true;
        private const float _defaultFontSize = 12;
        private const string _defaultFontFamily = "Segoe UI";
        private const float _pointSymbolSize = 6;
        private int _maxCurveCount;
        private bool _showPointSymbols;
        private float _fontSize;
        private string _fontFamily;
        private bool _closeButtonEnabled;
        private CurveItem _selectedCurve;
        private bool _highlighted;
        private Color _borderColor;
        private Color _highlightColor;


        // Properties                                                                                                               
        public ZedGraphControl Graph { get { return zedGraph; } }
        public GraphPane Pane { get { return zedGraph.GraphPane; } }
        public int CurveCount { get { return zedGraph.GraphPane.CurveList.Count; } }
        public int MaxCurveCount
        {
            get { return _maxCurveCount; }
            set { _maxCurveCount = value < 1 ? 1 : value; }
        }
        public CurveItem SelectedCurve { get { return _selectedCurve; } set { SelectCurve(value); } }
        public bool ShowPointSymbols
        {
            get { return _showPointSymbols; }
            set
            {
                if (value == _showPointSymbols) return;
                _showPointSymbols = value;
                UpdateCurveStyles();
                zedGraph.Invalidate();
            }
        }
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
        public event Action<XYPlot> PlotChangedEvent;
        public event Action<XYPlot> CurveSelectedEvent;


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
            _maxCurveCount = 10;
            _showPointSymbols = _defaultShowPointSymbols;
            _fontSize = _defaultFontSize;
            _fontFamily = _defaultFontFamily;
            _selectedCurve = null;
            _highlighted = false;
            _borderColor = SystemColors.ControlDark;
            _highlightColor = SystemColors.Highlight;
            //
            InitializeGraphPane();
            //
            panPlot.MouseDown += Child_MouseDown;
            zedGraph.MouseDownEvent += ZedGraph_MouseDownEvent;
            zedGraph.DoubleClickEvent += ZedGraph_DoubleClickEvent;
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
            }
            catch
            {
            }
        }
        private bool ZedGraph_MouseDownEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            try
            {
                SelectedEvent?.Invoke(this);
                //
                zedGraph.Focus();
                SelectCurve(GetCurveAtPoint(e.Location));
            }
            catch
            {
            }
            return false;
        }
        private bool ZedGraph_DoubleClickEvent(ZedGraphControl sender, MouseEventArgs e)
        {
            try
            {
                CurveItem curve = GetCurveAtPoint(e.Location);
                if (curve != null) EditCurve(curve);
                else EditPlot();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            return true;
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
                RemoveHiddenMenuItems(menuStrip);
                //
                int index = menuStrip.Items.Count;
                menuStrip.Items.Insert(index++, new ToolStripSeparator());
                //
                CurveItem curve = GetCurveAtPoint(mousePt);
                ToolStripMenuItem tsmiDeleteCurve = new ToolStripMenuItem("Delete Curve");
                tsmiDeleteCurve.Tag = curve;
                tsmiDeleteCurve.Enabled = curve != null;
                tsmiDeleteCurve.Click += DeleteCurve_Click;
                menuStrip.Items.Insert(index++, tsmiDeleteCurve);
                //
                ToolStripMenuItem tsmiDeleteAllCurves = new ToolStripMenuItem("Delete All Curves");
                tsmiDeleteAllCurves.Enabled = zedGraph.GraphPane.CurveList.Count > 0;
                tsmiDeleteAllCurves.Click += DeleteAllCurves_Click;
                menuStrip.Items.Insert(index++, tsmiDeleteAllCurves);
                //
                ToolStripMenuItem tsmiResetSettings = new ToolStripMenuItem("Reset Settings");
                tsmiResetSettings.Click += ResetSettings_Click;
                menuStrip.Items.Insert(index++, tsmiResetSettings);
                //
                ToolStripMenuItem tsmiResetAll = new ToolStripMenuItem("Reset All");
                tsmiResetAll.Click += ResetAll_Click;
                menuStrip.Items.Insert(index++, tsmiResetAll);
            }
            catch
            {
            }
        }
        private void RemoveHiddenMenuItems(ContextMenuStrip menuStrip)
        {
            string tag;
            for (int i = menuStrip.Items.Count - 1; i >= 0; i--)
            {
                tag = menuStrip.Items[i].Tag as string;
                if (tag != null && Array.IndexOf(_hiddenMenuItemTags, tag) >= 0) menuStrip.Items.RemoveAt(i);
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
        private void ResetSettings_Click(object sender, EventArgs e)
        {
            try
            {
                ResetSettings();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ResetAll_Click(object sender, EventArgs e)
        {
            try
            {
                ResetAll();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
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
            pane.XAxis.Title.Text = XYPlotSettings.DefaultXAxisTitle;
            pane.YAxis.Title.Text = XYPlotSettings.DefaultYAxisTitle;
            //
            pane.Legend.Position = LegendPos.TopCenter;
            pane.Legend.Border.IsVisible = false;
            pane.Legend.Fill = new Fill(Color.White);
            //
            UpdateFonts();
            //
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.Color = Color.Gainsboro;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.Color = Color.Gainsboro;
            pane.XAxis.MinorGrid.IsVisible = false;
            pane.YAxis.MinorGrid.IsVisible = false;
        }
        public bool AddCurve(string curveName, double[] xValues, double[] yValues)
        {
            GraphPane pane = zedGraph.GraphPane;
            if (pane.CurveList.Count >= _maxCurveCount) return false;
            //
            Color color = _curveColors[pane.CurveList.Count % _curveColors.Length];
            LineItem curve = pane.AddCurve(curveName, xValues, yValues, color, SymbolType.None);
            //
            XYPlotCurveSettings settings = new XYPlotCurveSettings();
            settings.Name = curveName;
            curve.Label.Text = GetSingleLineName(curveName);
            settings.Color = color;
            settings.LineWidth = _curveWidth;
            curve.Tag = settings;
            //
            UpdateCurveStyles();
            //
            pane.Legend.IsVisible = true;
            pane.AxisChange();
            //
            zedGraph.Invalidate();
            //
            PlotChangedEvent?.Invoke(this);
            return true;
        }
        public XYPlotState CaptureState()
        {
            XYPlotState state = new XYPlotState();
            state.Settings = GetPlotSettings();
            //
            GraphPane pane = zedGraph.GraphPane;
            foreach (CurveItem curve in pane.CurveList)
            {
                XYPlotCurveState curveState = new XYPlotCurveState();
                int count = curve.Points == null ? 0 : curve.Points.Count;
                double[] xValues = new double[count];
                double[] yValues = new double[count];
                for (int i = 0; i < count; i++)
                {
                    xValues[i] = curve.Points[i].X;
                    yValues[i] = curve.Points[i].Y;
                }
                curveState.XValues = xValues;
                curveState.YValues = yValues;
                curveState.Settings = new XYPlotCurveSettings(GetCurveSettings(curve));
                state.Curves.Add(curveState);
            }
            state.SelectedCurveIndex = _selectedCurve == null ? -1 : pane.CurveList.IndexOf(_selectedCurve);
            return state;
        }
        public bool RestoreState(XYPlotState state)
        {
            if (state == null) return true;
            //
            bool allCurvesRestored = true;
            Clear();
            foreach (XYPlotCurveState curveState in state.Curves)
            {
                if (curveState == null) continue;
                //
                int count = Math.Min(curveState.XValues.Length, curveState.YValues.Length);
                double[] xValues = new double[count];
                double[] yValues = new double[count];
                Array.Copy(curveState.XValues, xValues, count);
                Array.Copy(curveState.YValues, yValues, count);
                //
                if (!AddCurve(curveState.Settings.Name, xValues, yValues))
                {
                    allCurvesRestored = false;
                    break;
                }
                CurveItem curve = zedGraph.GraphPane.CurveList[zedGraph.GraphPane.CurveList.Count - 1];
                ApplyCurveSettings(curve, curveState.Settings);
            }
            //
            ApplyPlotSettings(state.Settings);
            if (state.SelectedCurveIndex >= 0 && state.SelectedCurveIndex < zedGraph.GraphPane.CurveList.Count)
                SelectCurve(zedGraph.GraphPane.CurveList[state.SelectedCurveIndex]);
            else SelectCurve(null);
            return allCurvesRestored;
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
            //
            CurveSelectedEvent?.Invoke(this);
        }
        private void UpdateCurveStyles()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            bool selected;
            int alpha;
            XYPlotCurveSettings settings;
            foreach (CurveItem item in pane.CurveList)
            {
                if (!(item is LineItem lineItem)) continue;
                //
                settings = GetCurveSettings(item);
                selected = item == _selectedCurve;
                alpha = _selectedCurve == null || selected ? 255 : _dimmedCurveAlpha;
                //
                lineItem.IsVisible = settings.Visible;
                lineItem.Line.Width = selected ? settings.LineWidth + _selectedCurveWidthIncrement : settings.LineWidth;
                lineItem.Line.Style = settings.GetDashStyle();
                lineItem.Line.Color = Color.FromArgb(alpha, settings.Color);
                //
                lineItem.Symbol.Type = _showPointSymbols ? SymbolType.Circle : SymbolType.None;
                lineItem.Symbol.Size = _pointSymbolSize;
                lineItem.Symbol.Border.Color = lineItem.Line.Color;
                lineItem.Symbol.Fill = new Fill(lineItem.Line.Color);
                //
                if (lineItem.Label.FontSpec == null) lineItem.Label.FontSpec = new FontSpec(pane.Legend.FontSpec);
                UpdateFont(lineItem.Label.FontSpec);
                lineItem.Label.FontSpec.IsBold = selected;
            }
        }
        private void UpdateFonts()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            UpdateFont(pane.Legend.FontSpec);
            UpdateFont(pane.XAxis.Title.FontSpec);
            UpdateFont(pane.XAxis.Scale.FontSpec);
            UpdateFont(pane.YAxis.Title.FontSpec);
            UpdateFont(pane.YAxis.Scale.FontSpec);
        }
        private void UpdateFont(FontSpec fontSpec)
        {
            fontSpec.Family = _fontFamily;
            fontSpec.Size = _fontSize;
        }
        private string GetSingleLineName(string name)
        {
            if (name == null) return "";
            return name.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
        }
        private XYPlotCurveSettings GetCurveSettings(CurveItem curve)
        {
            XYPlotCurveSettings settings = curve.Tag as XYPlotCurveSettings;
            if (settings == null)
            {
                settings = new XYPlotCurveSettings();
                settings.Name = curve.Label == null ? "" : curve.Label.Text;
                if (curve is LineItem lineItem)
                {
                    settings.Color = lineItem.Line.Color;
                    settings.LineWidth = lineItem.Line.Width;
                    settings.SetDashStyle(lineItem.Line.Style);
                }
                settings.Visible = curve.IsVisible;
                curve.Tag = settings;
            }
            return settings;
        }
        public void EditCurve(CurveItem curve)
        {
            if (curve == null || !zedGraph.GraphPane.CurveList.Contains(curve)) return;
            //
            XYPlotCurveSettings originalSettings = new XYPlotCurveSettings(GetCurveSettings(curve));
            XYPlotCurveSettings editedSettings = new XYPlotCurveSettings(originalSettings);
            //
            using (FrmXYPlotSettings frmSettings = new FrmXYPlotSettings())
            {
                if (frmSettings.ShowSettings(this, editedSettings, "Curve Settings",
                                             () => ApplyCurveSettings(curve, editedSettings)) != DialogResult.OK)
                {
                    ApplyCurveSettings(curve, originalSettings);
                }
            }
        }
        private void ApplyCurveSettings(CurveItem curve, XYPlotCurveSettings settings)
        {
            XYPlotCurveSettings curveSettings = GetCurveSettings(curve);
            curveSettings.CopyFrom(settings);
            //
            curve.Label.Text = GetSingleLineName(curveSettings.Name);
            //
            UpdateCurveStyles();
            zedGraph.GraphPane.AxisChange();
            zedGraph.Invalidate();
            //
            PlotChangedEvent?.Invoke(this);
        }
        public void EditPlot()
        {
            XYPlotSettings originalSettings = GetPlotSettings();
            XYPlotSettings editedSettings = new XYPlotSettings(originalSettings);
            //
            using (FrmXYPlotSettings frmSettings = new FrmXYPlotSettings())
            {
                if (frmSettings.ShowSettings(this, editedSettings, "Plot Settings",
                                             () => ApplyPlotSettings(editedSettings)) != DialogResult.OK)
                {
                    ApplyPlotSettings(originalSettings);
                }
            }
        }
        private XYPlotSettings GetPlotSettings()
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            XYPlotSettings settings = new XYPlotSettings();
            settings.XAxisTitle = pane.XAxis.Title.Text;
            settings.XAxisAutoScale = pane.XAxis.Scale.MinAuto && pane.XAxis.Scale.MaxAuto;
            settings.XAxisMin = pane.XAxis.Scale.Min;
            settings.XAxisMax = pane.XAxis.Scale.Max;
            settings.YAxisTitle = pane.YAxis.Title.Text;
            settings.YAxisAutoScale = pane.YAxis.Scale.MinAuto && pane.YAxis.Scale.MaxAuto;
            settings.YAxisMin = pane.YAxis.Scale.Min;
            settings.YAxisMax = pane.YAxis.Scale.Max;
            settings.ShowLegend = pane.Legend.IsVisible;
            settings.SetLegendPos(pane.Legend.Position);
            settings.ShowPointSymbols = _showPointSymbols;
            settings.SetFontFamilyName(_fontFamily);
            settings.FontSize = _fontSize;
            //
            return settings;
        }
        private XYPlotSettings GetDefaultPlotSettings()
        {
            XYPlotSettings settings = new XYPlotSettings();
            settings.ShowPointSymbols = _defaultShowPointSymbols;
            settings.FontSize = _defaultFontSize;
            return settings;
        }
        public void ResetSettings()
        {
            GraphPane pane = zedGraph.GraphPane;
            pane.ZoomStack.Clear();
            //
            bool curveSelectionChanged = _selectedCurve != null;
            _selectedCurve = null;
            //
            for (int i = 0; i < pane.CurveList.Count; i++)
            {
                CurveItem curve = pane.CurveList[i];
                XYPlotCurveSettings curveSettings = GetCurveSettings(curve);
                string curveName = curveSettings.Name;
                //
                curveSettings.CopyFrom(new XYPlotCurveSettings());
                curveSettings.Name = curveName;
                curveSettings.Color = _curveColors[i % _curveColors.Length];
                curveSettings.LineWidth = _curveWidth;
                curve.Label.Text = GetSingleLineName(curveSettings.Name);
            }
            // Apply once so the plot emits a single changed event
            ApplyPlotSettings(GetDefaultPlotSettings());
            if (curveSelectionChanged) CurveSelectedEvent?.Invoke(this);
        }
        public void ResetAll()
        {
            GraphPane pane = zedGraph.GraphPane;
            bool curveSelectionChanged = _selectedCurve != null;
            //
            pane.CurveList.Clear();
            pane.GraphObjList.Clear();
            _selectedCurve = null;
            //
            ResetSettings();
            if (curveSelectionChanged) CurveSelectedEvent?.Invoke(this);
        }
        private void ApplyPlotSettings(XYPlotSettings settings)
        {
            GraphPane pane = zedGraph.GraphPane;
            //
            pane.XAxis.Title.Text = settings.XAxisTitle;
            pane.YAxis.Title.Text = settings.YAxisTitle;
            pane.Legend.IsVisible = settings.ShowLegend;
            pane.Legend.Position = settings.GetLegendPos();
            _showPointSymbols = settings.ShowPointSymbols;
            _fontFamily = settings.GetFontFamilyName();
            _fontSize = settings.FontSize;
            //
            UpdateFonts();
            UpdateCurveStyles();
            // The axis ranges must be set after the automatic ranges are recomputed
            pane.AxisChange();
            //
            if (settings.XAxisAutoScale)
            {
                pane.XAxis.Scale.MinAuto = true;
                pane.XAxis.Scale.MaxAuto = true;
            }
            else
            {
                pane.XAxis.Scale.Min = settings.XAxisMin;
                pane.XAxis.Scale.Max = settings.XAxisMax;
            }
            if (settings.YAxisAutoScale)
            {
                pane.YAxis.Scale.MinAuto = true;
                pane.YAxis.Scale.MaxAuto = true;
            }
            else
            {
                pane.YAxis.Scale.Min = settings.YAxisMin;
                pane.YAxis.Scale.Max = settings.YAxisMax;
            }
            pane.AxisChange();
            //
            zedGraph.Invalidate();
            //
            PlotChangedEvent?.Invoke(this);
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
            return GetNearestCurveToPoint(pane, mousePt);
        }
        private CurveItem GetNearestCurveToPoint(GraphPane pane, Point mousePt)
        {
            CurveItem nearestCurve = null;
            double distance;
            double minDistance = _curveHitTolerance;
            //
            try
            {
                PointF current;
                PointF previous = PointF.Empty;
                bool previousValid;
                Axis yAxis;
                foreach (CurveItem curve in pane.CurveList)
                {
                    if (!curve.IsVisible || curve.Points == null) continue;
                    //
                    yAxis = curve.IsY2Axis ? (Axis)pane.Y2Axis : pane.YAxis;
                    previousValid = false;
                    for (int i = 0; i < curve.Points.Count; i++)
                    {
                        PointPair point = curve.Points[i];
                        if (point.IsInvalid)
                        {
                            previousValid = false;
                            continue;
                        }
                        current = new PointF(pane.XAxis.Scale.Transform(point.X), yAxis.Scale.Transform(point.Y));
                        //
                        if (previousValid) distance = GetDistanceToSegment(mousePt, previous, current);
                        else distance = GetDistanceToPoint(mousePt, current);
                        //
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestCurve = curve;
                        }
                        //
                        previous = current;
                        previousValid = true;
                    }
                }
            }
            catch
            {
                return null;
            }
            //
            return nearestCurve;
        }
        private double GetDistanceToPoint(PointF point1, PointF point2)
        {
            double dx = point1.X - point2.X;
            double dy = point1.Y - point2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        private double GetDistanceToSegment(PointF point, PointF start, PointF end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double lengthSquare = dx * dx + dy * dy;
            if (lengthSquare <= 0) return GetDistanceToPoint(point, start);
            //
            double t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquare;
            if (t < 0) t = 0;
            else if (t > 1) t = 1;
            //
            return GetDistanceToPoint(point, new PointF((float)(start.X + t * dx), (float)(start.Y + t * dy)));
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
                pane.XAxis.Title.Text = XYPlotSettings.DefaultXAxisTitle;
                pane.YAxis.Title.Text = XYPlotSettings.DefaultYAxisTitle;
            }
            pane.AxisChange();
            //
            zedGraph.Invalidate();
            //
            PlotChangedEvent?.Invoke(this);
        }
        public void SaveAsImage()
        {
            zedGraph.SaveAs();
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
