// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeJob;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ZedGraph;

namespace UserControls
{
    public partial class AnalysisProgressView : UserControl
    {
        // Variables                                                                                                                
        private const string IncrementSizeName = "Increment size";
        private const string IterationCountName = "Iteration count";
        private const string ResidualForceName = "Residual force";
        private const string DisplacementCorrectionName = "Displacement correction";
        private const string ResidualFluxName = "Residual flux";
        private const string TemperatureCorrectionName = "Temperature correction";
        private const string ContactElementCountName = "Contact elements";
        // Largest span (in decades) the percent (log) axis is allowed to show. Convergence data can
        // legitimately span ~19 decades (e.g. 1e2 down to 1e-16), so this must be wide enough to show
        // it all; but ZedGraph's log wheel-zoom becomes unstable past ~28 decades (the minimum
        // suddenly jumps up by ~20 decades once it drops below ~1e-21). 24 keeps a safe margin below
        // that while still showing the full data plus a few decades of zoom-out headroom.
        private const double MaxConvergenceDecades = 24;
        // Smallest value the percent (log) axis minimum may reach. ZedGraph's log PanScale overflows
        // and inverts the axis (min > max) once the minimum is pushed below ~1e-20, so this floor
        // (two decades of margin) is enforced on zoom, pan and auto-scaling alike.
        private const double MinConvergenceValue = 1e-18;
        //
        private CalculixStatusData _statusData;
        private CalculixConvergenceData _convergenceData;
        private string _timeUnit;
        // Cached reflection handle to ZedGraph's private "_isPanning" flag, used to bound the
        // convergence log axis mid-pan (ZedGraph updates the pan continuously but only raises
        // ZoomEvent on mouse-up). Null if the field is ever absent, in which case the mid-pan guard
        // is skipped and only the mouse-up guard applies.
        private System.Reflection.FieldInfo _isPanningField;


        // Constructors                                                                                                             
        public AnalysisProgressView()
        {
            _timeUnit = "";
            InitializeComponent();
            ConfigureStatusGraph();
            ConfigureConvergenceGraph();
            ClearData();
        }


        // Methods                                                                                                                  
        private void ConfigureStatusGraph()
        {
            GraphPane pane = _statusGraph.GraphPane;
            ConfigureGraphPane(pane);
            pane.Title.Text = "Status";
            UpdateTimeUnitLabels();
            pane.Y2Axis.Title.Text = "Iterations";
            pane.Y2Axis.IsVisible = true;
            pane.Y2Axis.Title.FontSpec.Size = 12;
            pane.Y2Axis.Scale.FontSpec.Size = 12;
            pane.XAxis.Scale.Min = 0;
            pane.XAxis.Scale.MinAuto = false;
            pane.YAxis.Scale.Min = 0;
            pane.YAxis.Scale.MinAuto = false;
            pane.Y2Axis.Scale.Min = 0;
            pane.Y2Axis.Scale.MinAuto = false;
            _statusGraph.PointValueEvent += StatusGraph_PointValueEvent;
            _statusGraph.ContextMenuBuilder += Graph_ContextMenuBuilder;
        }

        private void ConfigureConvergenceGraph()
        {
            GraphPane pane = _convergenceGraph.GraphPane;
            ConfigureGraphPane(pane);
            pane.Title.Text = "Convergence";
            pane.XAxis.Title.Text = "Comulative iteration";
            pane.YAxis.Title.Text = "Percent (log)";
            pane.YAxis.Type = AxisType.Log;
            pane.YAxis.Scale.MajorStep = 3;
            pane.YAxis.Scale.MajorStepAuto = false;
            pane.YAxis.Scale.MinAuto = true;
            pane.Y2Axis.Title.Text = "Contact elements";
            pane.Y2Axis.IsVisible = false;
            pane.Y2Axis.Title.FontSpec.Size = 12;
            pane.Y2Axis.Scale.FontSpec.Size = 12;
            pane.Y2Axis.Scale.Min = 0;
            pane.Y2Axis.Scale.MinAuto = false;
            pane.XAxis.Scale.Min = 1;
            pane.XAxis.Scale.MinAuto = false;
            _convergenceGraph.PointValueEvent += ConvergenceGraph_PointValueEvent;
            _convergenceGraph.ContextMenuBuilder += Graph_ContextMenuBuilder;
            _convergenceGraph.ZoomEvent += Convergence_ZoomEvent;
            _convergenceGraph.MouseMove += Convergence_MouseMove;
        }

        private void ConfigureGraphPane(GraphPane pane)
        {
            pane.IsFontsScaled = false;
            pane.IsPenWidthScaled = false;
            pane.Fill = new Fill(SystemColors.Control);
            pane.Chart.Fill = new Fill(Color.White);
            pane.Title.FontSpec.Size = 12;
            pane.Legend.Position = LegendPos.TopCenter;
            pane.Legend.IsVisible = true;
            pane.Legend.FontSpec.Size = 12;
            pane.XAxis.Title.FontSpec.Size = 12;
            pane.XAxis.Scale.FontSpec.Size = 12;
            pane.YAxis.Title.FontSpec.Size = 12;
            pane.YAxis.Scale.FontSpec.Size = 12;
            pane.YAxis.MinorTic.IsAllTics = false;
            pane.YAxis.MinorGrid.IsVisible = false;
            pane.Y2Axis.MinorTic.IsAllTics = false;
            pane.Y2Axis.MinorGrid.IsVisible = false;
            pane.XAxis.MajorGrid.IsVisible = true;
            pane.XAxis.MajorGrid.Color = Color.Gainsboro;
            pane.YAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.Color = Color.Gainsboro;
        }

        public void ClearData()
        {
            _statusData = null;
            _convergenceData = null;
            //
            GraphPane statusPane = _statusGraph.GraphPane;
            statusPane.CurveList.Clear();
            statusPane.Title.Text = "Status - waiting for data";
            ResetStatusScale();
            _statusGraph.Invalidate();
            //
            GraphPane convergencePane = _convergenceGraph.GraphPane;
            convergencePane.CurveList.Clear();
            convergencePane.Y2Axis.IsVisible = false;
            convergencePane.Title.Text = "Convergence - waiting for data";
            ResetConvergenceScale();
            _convergenceGraph.Invalidate();
        }

        public void SetTimeUnit(string timeUnit)
        {
            _timeUnit = timeUnit ?? "";
            UpdateTimeUnitLabels();
            _statusGraph.Invalidate();
        }

        private void UpdateTimeUnitLabels()
        {
            string unit = _timeUnit.Length > 0 ? " [" + _timeUnit + "]" : "";
            GraphPane pane = _statusGraph.GraphPane;
            pane.XAxis.Title.Text = "Total analysis time" + unit;
            pane.YAxis.Title.Text = "Increment size" + unit;
        }

        public void SetStatusData(CalculixStatusData data)
        {
            _statusData = data;
            GraphPane pane = _statusGraph.GraphPane;
            pane.CurveList.Clear();
            bool incrementMaximumAuto = pane.YAxis.Scale.MaxAuto;
            bool iterationMaximumAuto = pane.Y2Axis.Scale.MaxAuto;
            bool iterationStepAuto = pane.Y2Axis.Scale.MajorStepAuto;

            if (data == null || data.Count == 0)
            {
                pane.Title.Text = "Status - waiting for data";
            }
            else
            {
                pane.Title.Text = "Status";
                LineItem incrementCurve = pane.AddCurve(IncrementSizeName, data.TotalTimes, data.IncrementTimes,
                                                        Color.DodgerBlue, SymbolType.Circle);
                LineItem iterationCurve = pane.AddCurve(IterationCountName, data.TotalTimes, data.IterationCounts,
                                                        Color.DarkOrange, SymbolType.Circle);
                iterationCurve.IsY2Axis = true;
                ConfigureCurve(incrementCurve);
                ConfigureCurve(iterationCurve);
                // ZedGraph places constant-value curves directly on the top plot border. Use a
                // temporary manual maximum to give auto-scaled live data visible headroom.
                if (incrementMaximumAuto)
                {
                    pane.YAxis.Scale.Max = GetMaximumWithHeadroom(data.IncrementTimes);
                    pane.YAxis.Scale.MaxAuto = false;
                }
                if (iterationMaximumAuto)
                {
                    pane.Y2Axis.Scale.Max = GetIterationMaximumWithHeadroom(data.IterationCounts);
                    pane.Y2Axis.Scale.MaxAuto = false;
                }
            }

            _statusGraph.AxisChange();
            // Snap the iteration (right) axis ticks onto the increment-size (left) axis grid lines.
            if (iterationMaximumAuto && data != null && data.Count > 0)
                AlignY2ToYGridLines(pane, GetMaximum(data.IterationCounts));
            pane.YAxis.Scale.MaxAuto = incrementMaximumAuto;
            pane.Y2Axis.Scale.MaxAuto = iterationMaximumAuto;
            pane.Y2Axis.Scale.MajorStepAuto = iterationStepAuto;
            _statusGraph.Invalidate();
        }

        public void SetConvergenceData(CalculixConvergenceData data)
        {
            _convergenceData = data;
            GraphPane pane = _convergenceGraph.GraphPane;
            pane.CurveList.Clear();
            pane.Y2Axis.IsVisible = false;
            bool contactMaximumAuto = pane.Y2Axis.Scale.MaxAuto;
            bool contactStepAuto = pane.Y2Axis.Scale.MajorStepAuto;
            bool percentMinimumAuto = pane.YAxis.Scale.MinAuto;

            if (data == null || data.Count == 0)
            {
                pane.Title.Text = "Convergence - waiting for data";
            }
            else
            {
                pane.Title.Text = "Convergence";
                AddLogCurve(pane, ResidualForceName, data.RecordNumbers, data.ResidualForces, Color.Red);
                AddLogCurve(pane, DisplacementCorrectionName, data.RecordNumbers, data.DisplacementCorrections,
                            Color.DodgerBlue);
                AddLogCurve(pane, ResidualFluxName, data.RecordNumbers, data.ResidualFluxes, Color.ForestGreen);
                AddLogCurve(pane, TemperatureCorrectionName, data.RecordNumbers, data.TemperatureCorrections,
                            Color.DarkViolet);
                if (ContainsPositiveValue(data.ContactElementCounts))
                {
                    pane.Y2Axis.IsVisible = true;
                    LineItem contactCurve = pane.AddCurve(ContactElementCountName, data.RecordNumbers,
                                                          data.ContactElementCounts, Color.Black, SymbolType.Circle);
                    contactCurve.IsY2Axis = true;
                    ConfigureCurve(contactCurve);
                    if (contactMaximumAuto)
                    {
                        pane.Y2Axis.Scale.Max = GetIterationMaximumWithHeadroom(data.ContactElementCounts);
                        pane.Y2Axis.Scale.MaxAuto = false;
                    }
                }
            }

            _convergenceGraph.AxisChange();
            // Keep the auto-scaled percent axis from ballooning down to an ultra-small outlier value.
            if (percentMinimumAuto) CapConvergenceYAxisMin(pane);
            // Snap the contact-element (right) axis ticks onto the percent (left) axis grid lines.
            if (contactMaximumAuto && pane.Y2Axis.IsVisible && data != null && data.Count > 0)
                AlignY2ToYGridLines(pane, GetMaximum(data.ContactElementCounts));
            pane.YAxis.Scale.MinAuto = percentMinimumAuto;
            pane.Y2Axis.Scale.MaxAuto = contactMaximumAuto;
            pane.Y2Axis.Scale.MajorStepAuto = contactStepAuto;
            _convergenceGraph.Invalidate();
        }

        private void AddLogCurve(GraphPane pane, string name, double[] x, double[] y, Color color)
        {
            if (!ContainsPositiveValue(y)) return;

            PointPairList points = new PointPairList();
            int count = Math.Min(x.Length, y.Length);
            for (int i = 0; i < count; i++)
            {
                double value = y[i];
                if (value > 0 && !double.IsInfinity(value) && !double.IsNaN(value))
                    points.Add(new PointPair(x[i], value, 0, i));
                else
                    points.Add(new PointPair(x[i], PointPairBase.Missing, 0, i));
            }

            LineItem curve = pane.AddCurve(name, points, color, SymbolType.Circle);
            ConfigureCurve(curve);
        }

        private void ConfigureCurve(LineItem curve)
        {
            curve.Line.IsAntiAlias = true;
            curve.Line.Width = 1.5F;
            curve.Symbol.Fill = new Fill(curve.Color);
            curve.Symbol.Size = 4;
        }

        private double GetMaximumWithHeadroom(double[] values)
        {
            double maximum = GetMaximum(values);

            if (maximum <= 0) return 1;
            else return 1.1 * maximum;
        }

        private double GetIterationMaximumWithHeadroom(double[] values)
        {
            double maximum = GetMaximum(values);
            if (maximum <= 0) return 1;
            else return Math.Ceiling(maximum) + 1;
        }

        // Aligns the secondary (right) Y axis so its major tick marks fall on the primary (left)
        // Y axis major grid lines. Both axes share the same bottom (fraction 0 of the chart
        // height), so giving one right-axis major step the same fractional height as one
        // left-axis major step makes every right tick coincide with a left grid line. The right
        // axis is linear and anchored at zero; its maximum is stretched to whatever the alignment
        // requires, which is always at least the data maximum so no data point gets clipped.
        private void AlignY2ToYGridLines(GraphPane pane, double dataMaximum)
        {
            // Drop any tick anchor left behind by a previous zoom re-alignment so the right axis
            // falls back to the natural zero-anchored lattice for this round-number alignment.
            pane.Y2Axis.Scale.BaseTic = PointPairBase.Missing;
            if (dataMaximum <= 0) return;
            //
            Scale yScale = pane.YAxis.Scale;
            // Left grid lines are evenly spaced in the axis transform space (value for a linear
            // axis, log10(value) for a log axis), so the fractional spacing uses that same space.
            double yRange = pane.YAxis.Type == AxisType.Log
                            ? Math.Log10(yScale.Max) - Math.Log10(yScale.Min)
                            : yScale.Max - yScale.Min;
            if (yRange <= 0 || yScale.MajorStep <= 0) return;
            // Fraction of the chart height spanned by one primary-axis major step.
            double stepFraction = yScale.MajorStep / yRange;
            // Nothing to align to when the primary axis shows no interior grid lines.
            if (stepFraction <= 0 || stepFraction >= 1) return;
            // Pick a nice right-axis step whose fractional height matches the grid, then stretch
            // the right-axis maximum so that step spans exactly one grid interval.
            double y2Step = GetNiceStepAtLeast(dataMaximum * stepFraction);
            double y2Max = y2Step / stepFraction;
            //
            Scale y2Scale = pane.Y2Axis.Scale;
            y2Scale.Min = 0;
            y2Scale.MinAuto = false;
            y2Scale.Max = y2Max;
            y2Scale.MaxAuto = false;
            y2Scale.MajorStep = y2Step;
            y2Scale.MajorStepAuto = false;
        }

        // Smallest 1-2-5 x 10^k value greater than or equal to the given positive value.
        private double GetNiceStepAtLeast(double value)
        {
            if (value <= 0) return 1;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double normalized = value / magnitude;   // within [1, 10)
            double nice;
            if (normalized <= 1) nice = 1;
            else if (normalized <= 2) nice = 2;
            else if (normalized <= 5) nice = 5;
            else nice = 10;
            return nice * magnitude;
        }

        private void Convergence_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            GraphPane pane = _convergenceGraph.GraphPane;
            // Reject any zoom OR pan step that pushes the log axis outside its safe region: restore
            // every axis to the pre-step state so navigation simply stops at the limit rather than
            // distorting the plot or breaking the log axis. This fires for wheel/rubber-band zoom and
            // for pan (HandlePanFinish raises ZoomEvent on mouse-up). Zoom-in and pan-up stay inside
            // the region, so they are never rejected.
            if (oldState != null && ConvergenceViewOutOfBounds(pane))
            {
                oldState.ApplyState(pane);
                _convergenceGraph.Invalidate();
                return;
            }
            if (pane.Y2Axis.IsVisible && _convergenceData != null && _convergenceData.Count > 0)
            {
                RealignY2OnZoom(pane, GetMaximum(_convergenceData.ContactElementCounts));
                _convergenceGraph.Invalidate();
            }
        }

        // Limits the auto-scaled percent (log) axis so an ultra-small converged value cannot stretch
        // the range past the safe span or below the safe floor. The maximum stays data driven; only the
        // minimum is lifted. Values below the resulting window clip to the bottom of the plot.
        private void CapConvergenceYAxisMin(GraphPane pane)
        {
            Scale yScale = pane.YAxis.Scale;
            if (yScale.Min <= 0 || yScale.Max <= 0) return;
            double minimumLimit = Math.Max(MinConvergenceValue, yScale.Max / Math.Pow(10, MaxConvergenceDecades));
            if (yScale.Min < minimumLimit)
            {
                yScale.Min = minimumLimit;
                yScale.MinAuto = false;
            }
        }

        private void Convergence_MouseMove(object sender, MouseEventArgs e)
        {
            // ZedGraph pans continuously during the drag (HandlePanDrag) but only raises ZoomEvent on
            // mouse-up, and its PanScale has no bounds checking - so without intervening here the log
            // axis overflows and inverts once the minimum passes ~1e-20, and the right-axis ticks drift
            // off the (re-anchoring) log grid until release. Because the pan is incremental, clamping
            // and re-aligning after each step keeps the minimum pinned at the floor and the axes in sync.
            if (!IsConvergencePanning()) return;
            //
            GraphPane pane = _convergenceGraph.GraphPane;
            bool changed = ClampConvergencePanMinimum(pane);
            if (pane.Y2Axis.IsVisible && _convergenceData != null && _convergenceData.Count > 0)
            {
                RealignY2OnZoom(pane, GetMaximum(_convergenceData.ContactElementCounts));
                changed = true;
            }
            if (changed) _convergenceGraph.Refresh();
        }

        private bool IsConvergencePanning()
        {
            if (_isPanningField == null)
                _isPanningField = typeof(ZedGraphControl).GetField("_isPanning",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return _isPanningField != null && (bool)_isPanningField.GetValue(_convergenceGraph);
        }

        // Pins the percent (log) axis minimum at the safe floor during a pan and shifts the linear right
        // axis by the same amount, so panning down stops cleanly at the floor with both axes coupled
        // instead of overflowing ZedGraph's log PanScale. Panning up needs no guard.
        private bool ClampConvergencePanMinimum(GraphPane pane)
        {
            Scale yScale = pane.YAxis.Scale;
            if (yScale.Min <= 0 || yScale.Max <= 0 || yScale.Min >= MinConvergenceValue) return false;
            double logMin = Math.Log10(yScale.Min);
            double logMax = Math.Log10(yScale.Max);
            double shift = Math.Log10(MinConvergenceValue) - logMin;   // decades to shift the window up
            yScale.Min = MinConvergenceValue;
            yScale.Max = Math.Pow(10, logMax + shift);
            yScale.MinAuto = false;
            yScale.MaxAuto = false;
            // Shift the coupled linear right axis by the same fraction of the plot height.
            Scale y2Scale = pane.Y2Axis.Scale;
            double span = logMax - logMin;
            if (pane.Y2Axis.IsVisible && span > 0 && y2Scale.Max > y2Scale.Min)
            {
                double y2Shift = (shift / span) * (y2Scale.Max - y2Scale.Min);
                y2Scale.Min += y2Shift;
                y2Scale.Max += y2Shift;
                y2Scale.MinAuto = false;
                y2Scale.MaxAuto = false;
            }
            return true;
        }

        // True when the percent (log) axis has left the region where ZedGraph's log zoom/pan stays
        // stable: an inverted or degenerate range (min >= max, the pan-overflow break state), a minimum
        // pushed below the safe floor, or a span wide enough to destabilise wheel-zoom. The caller
        // rejects such a step by restoring the pre-step state.
        private bool ConvergenceViewOutOfBounds(GraphPane pane)
        {
            Scale yScale = pane.YAxis.Scale;
            if (yScale.Min <= 0 || yScale.Max <= yScale.Min) return true;
            if (yScale.Min < MinConvergenceValue) return true;
            return Math.Log10(yScale.Max) - Math.Log10(yScale.Min) > MaxConvergenceDecades;
        }

        // Re-applies the right-axis grid-line alignment after a zoom or pan, when the left (log) axis
        // may no longer start on a whole decade. ZedGraph re-anchors the log grid lines to
        // Ceiling(log10(min)), so the lowest grid line sits a fraction phi above the axis bottom; the
        // right axis tick anchor (BaseTic) is placed at that same fraction so every right tick keeps
        // landing on a grid line. While zoomed off the decades the right labels are no longer round
        // numbers - that is the accepted trade-off for staying aligned.
        private void RealignY2OnZoom(GraphPane pane, double dataMaximum)
        {
            Scale yScale = pane.YAxis.Scale;
            if (yScale.Min <= 0 || yScale.Max <= 0 || yScale.MajorStep <= 0) return;
            double tMin = Math.Log10(yScale.Min);
            double tMax = Math.Log10(yScale.Max);
            double tRange = tMax - tMin;
            if (tRange <= 0) return;
            // The lowest whole decade at or above the axis minimum - ZedGraph's log base tick.
            double baseTicLog = Math.Ceiling(tMin - 1e-9);
            double phi = (baseTicLog - tMin) / tRange;         // fraction of the lowest grid line
            double stepFraction = yScale.MajorStep / tRange;   // fractional grid spacing
            if (phi < 0 || phi >= 1) return;                   // no interior grid line to align to
            //
            Scale y2Scale = pane.Y2Axis.Scale;
            // Bottom grid line on the axis and the right axis still anchored at zero: this is the
            // vertically un-zoomed case, so keep the tidy round-number alignment.
            if (phi < 1e-6 && Math.Abs(y2Scale.Min) < 1e-9)
            {
                AlignY2ToYGridLines(pane, dataMaximum);
                return;
            }
            // Zoomed off the decades: match the exact grid phase with the current right-axis range.
            double y2Range = y2Scale.Max - y2Scale.Min;
            if (y2Range <= 0) return;
            y2Scale.MajorStep = stepFraction * y2Range;
            y2Scale.MajorStepAuto = false;
            y2Scale.BaseTic = y2Scale.Min + phi * y2Range;
        }

        private void Graph_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt,
                                              ZedGraphControl.ContextMenuObjectState objState)
        {
            // Replace the built-in "Set Scale to Default" item. The built-in reset flips every axis
            // back to fully auto and redraws directly, bypassing the Y2 grid-line alignment; route it
            // through our own reset instead so the alignment survives.
            for (int i = 0; i < menuStrip.Items.Count; i++)
            {
                if (menuStrip.Items[i].Tag as string != "set_default") continue;
                //
                ToolStripMenuItem defaultItem = menuStrip.Items[i] as ToolStripMenuItem;
                if (defaultItem == null) break;
                ToolStripMenuItem newItem = new ToolStripMenuItem(defaultItem.Text) { Tag = "set_default" };
                newItem.Click += (s, e) => RestoreDefaultScale(sender);
                menuStrip.Items.RemoveAt(i);
                menuStrip.Items.Insert(i, newItem);
                break;
            }
        }

        private void RestoreDefaultScale(ZedGraphControl graph)
        {
            graph.GraphPane.ZoomStack.Clear();
            if (graph == _statusGraph)
            {
                ResetStatusScale();
                SetStatusData(_statusData);
            }
            else if (graph == _convergenceGraph)
            {
                ResetConvergenceScale();
                SetConvergenceData(_convergenceData);
            }
        }

        private double GetMaximum(double[] values)
        {
            double maximum = 0;
            foreach (double value in values)
            {
                if (!double.IsInfinity(value) && !double.IsNaN(value)) maximum = Math.Max(maximum, value);
            }
            return maximum;
        }

        private bool ContainsPositiveValue(double[] values)
        {
            foreach (double value in values)
            {
                if (value > 0 && !double.IsInfinity(value) && !double.IsNaN(value)) return true;
            }
            return false;
        }

        private string StatusGraph_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int index)
        {
            if (_statusData == null || index < 0 || index >= _statusData.Count) return "";

            double value;
            if (curve.Label.Text == IterationCountName) value = _statusData.IterationCounts[index];
            else value = _statusData.IncrementTimes[index];
            string timeUnit = _timeUnit.Length > 0 ? " " + _timeUnit : "";
            string valueUnit = curve.Label.Text == IterationCountName ? "" : timeUnit;

            return string.Format(CultureInfo.CurrentCulture,
                                 "Step {0}, increment {1}, attempt {2}\nTotal time: {3:G6}{4}\n{5}: {6:G6}{7}",
                                 _statusData.Steps[index], _statusData.Increments[index],
                                 _statusData.Attempts[index], _statusData.TotalTimes[index],
                                 timeUnit, curve.Label.Text, value, valueUnit);
        }

        private string ConvergenceGraph_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve,
                                                         int index)
        {
            if (_convergenceData == null || index < 0 || index >= curve.Points.Count) return "";

            double value;
            int dataIndex = index;
            bool percentage = true;
            if (curve.Label.Text == ContactElementCountName)
            {
                value = _convergenceData.ContactElementCounts[index];
                percentage = false;
            }
            else
            {
                if (curve.Points[index].Tag is int originalIndex) dataIndex = originalIndex;
                if (dataIndex < 0 || dataIndex >= _convergenceData.Count) return "";
                //
                if (curve.Label.Text == ResidualForceName) value = _convergenceData.ResidualForces[dataIndex];
                else if (curve.Label.Text == DisplacementCorrectionName)
                    value = _convergenceData.DisplacementCorrections[dataIndex];
                else if (curve.Label.Text == ResidualFluxName) value = _convergenceData.ResidualFluxes[dataIndex];
                else value = _convergenceData.TemperatureCorrections[dataIndex];
            }

            return string.Format(CultureInfo.CurrentCulture,
                                 "Step {0}, increment {1}, attempt {2}, iteration {3}\n{4}: {5:G6}{6}",
                                 _convergenceData.Steps[dataIndex], _convergenceData.Increments[dataIndex],
                                 _convergenceData.Attempts[dataIndex], _convergenceData.Iterations[dataIndex],
                                 curve.Label.Text, value, percentage ? "%" : "");
        }

        private void ResetStatusScale()
        {
            GraphPane pane = _statusGraph.GraphPane;
            pane.XAxis.Scale.Min = 0;
            pane.XAxis.Scale.MinAuto = false;
            pane.XAxis.Scale.MaxAuto = true;
            pane.YAxis.Scale.Min = 0;
            pane.YAxis.Scale.MinAuto = false;
            pane.YAxis.Scale.MaxAuto = true;
            pane.Y2Axis.Scale.Min = 0;
            pane.Y2Axis.Scale.MinAuto = false;
            pane.Y2Axis.Scale.MaxAuto = true;
        }

        private void ResetConvergenceScale()
        {
            GraphPane pane = _convergenceGraph.GraphPane;
            pane.XAxis.Scale.Min = 1;
            pane.XAxis.Scale.MinAuto = false;
            pane.XAxis.Scale.MaxAuto = true;
            pane.YAxis.Scale.MinAuto = true;
            pane.YAxis.Scale.MaxAuto = true;
            pane.Y2Axis.Scale.Min = 0;
            pane.Y2Axis.Scale.MinAuto = false;
            pane.Y2Axis.Scale.MaxAuto = true;
        }
    }
}
