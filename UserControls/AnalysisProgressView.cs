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
    public class AnalysisProgressView : UserControl
    {
        // Variables                                                                                                                
        private const string IncrementSizeName = "Increment size";
        private const string IterationCountName = "Iteration count";
        private const string ResidualForceName = "Residual force";
        private const string DisplacementCorrectionName = "Displacement correction";
        private const string ResidualFluxName = "Residual flux";
        private const string TemperatureCorrectionName = "Temperature correction";
        private const string ContactElementCountName = "Contact elements";
        //
        private TableLayoutPanel _tableLayoutPanel;
        private ZedGraphControl _statusGraph;
        private ZedGraphControl _convergenceGraph;
        private CalculixStatusData _statusData;
        private CalculixConvergenceData _convergenceData;
        private string _timeUnit;


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
        private void InitializeComponent()
        {
            SuspendLayout();
            //
            _tableLayoutPanel = new TableLayoutPanel();
            _statusGraph = CreateGraphControl("zedGraphStatus");
            _convergenceGraph = CreateGraphControl("zedGraphConvergence");
            //
            _tableLayoutPanel.ColumnCount = 1;
            _tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _tableLayoutPanel.Dock = DockStyle.Fill;
            _tableLayoutPanel.Name = "tableLayoutPanelProgress";
            _tableLayoutPanel.RowCount = 2;
            _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _tableLayoutPanel.TabIndex = 0;
            _statusGraph.Margin = new Padding(3, 3, 3, 2);
            _convergenceGraph.Margin = new Padding(3, 2, 3, 3);
            _tableLayoutPanel.Controls.Add(_statusGraph, 0, 0);
            _tableLayoutPanel.Controls.Add(_convergenceGraph, 0, 1);
            //
            Controls.Add(_tableLayoutPanel);
            Name = "AnalysisProgressView";
            Size = new Size(600, 300);
            //
            ResumeLayout(false);
        }

        private ZedGraphControl CreateGraphControl(string name)
        {
            ZedGraphControl graph = new ZedGraphControl();
            graph.Dock = DockStyle.Fill;
            graph.IsShowPointValues = true;
            graph.Name = name;
            graph.UseExtendedPrintDialog = true;
            return graph;
        }

        private void ConfigureStatusGraph()
        {
            GraphPane pane = _statusGraph.GraphPane;
            ConfigureGraphPane(pane);
            pane.Title.Text = "Status";
            UpdateTimeUnitLabels();
            pane.Y2Axis.Title.Text = "Number of iterations";
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
            pane.Y2Axis.Title.Text = "Number of contact elements";
            pane.Y2Axis.IsVisible = false;
            pane.Y2Axis.Title.FontSpec.Size = 12;
            pane.Y2Axis.Scale.FontSpec.Size = 12;
            pane.Y2Axis.Scale.Min = 0;
            pane.Y2Axis.Scale.MinAuto = false;
            pane.XAxis.Scale.Min = 1;
            pane.XAxis.Scale.MinAuto = false;
            _convergenceGraph.PointValueEvent += ConvergenceGraph_PointValueEvent;
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
            pane.YAxis.Scale.MaxAuto = incrementMaximumAuto;
            pane.Y2Axis.Scale.MaxAuto = iterationMaximumAuto;
            _statusGraph.Invalidate();
        }

        public void SetConvergenceData(CalculixConvergenceData data)
        {
            _convergenceData = data;
            GraphPane pane = _convergenceGraph.GraphPane;
            pane.CurveList.Clear();
            pane.Y2Axis.IsVisible = false;
            bool contactMaximumAuto = pane.Y2Axis.Scale.MaxAuto;

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
            pane.Y2Axis.Scale.MaxAuto = contactMaximumAuto;
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
