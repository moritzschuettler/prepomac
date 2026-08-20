// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeResults;
using System;
using System.Collections.Generic;
using UserControls;

namespace PrePoMax
{
    public partial class FrmMain
    {
        private XYPlotPanel _xyPlotPanel;

        private void tsmiXYPlotPanel_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized) return;
                //
                if (tsmiXYPlotPanel.Checked)
                {
                    tsmiXYPlotPanel.Checked = false;
                    HideSidePanel();
                }
                else ShowXYPlotPanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ShowXYPlotPanel()
        {
            if (_xyPlotPanel == null)
            {
                _xyPlotPanel = new XYPlotPanel();
                _xyPlotPanel.CloseButtonPressedEvent += tsmiXYPlotPanel_Click;
            }
            //
            if (!tsmiXYPlotPanel.Checked) ShowSidePanel(_xyPlotPanel, 0.5, 100, 1000, false);
        }
        private bool CanAddHistoryOutputToNewPlot()
        {
            return _xyPlotPanel == null || _xyPlotPanel.PlotCount < _xyPlotPanel.MaxPlotCount;
        }
        private void AddHistoryOutputsToPlot(HistoryResultData[] historyData)
        {
            try
            {
                ShowXYPlotPanel();
                //
                XYPlot plot = _xyPlotPanel.SelectedPlot;
                if (plot == null) plot = _xyPlotPanel.AddPlot();
                //
                AddHistoryOutputsToPlot(historyData, plot);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void AddHistoryOutputsToNewPlot(HistoryResultData[] historyData)
        {
            try
            {
                ShowXYPlotPanel();
                //
                XYPlot plot = _xyPlotPanel.AddPlot();
                if (plot == null) return;
                //
                AddHistoryOutputsToPlot(historyData, plot);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void AddHistoryOutputsToPlot(HistoryResultData[] historyData, XYPlot plot)
        {
            if (plot == null || historyData == null) return;
            //
            for (int i = 0; i < historyData.Length; i++) AddHistoryOutputToPlot(historyData[i], plot);
            //
            _xyPlotPanel.SelectedPlot = plot;
            _xyPlotPanel.UpdateDataGrid();
        }
        private void AddHistoryOutputToPlot(HistoryResultData historyData, XYPlot plot)
        {
            string[] columnNames;
            object[][] rowBasedData;
            _controller.GetHistoryOutputData(historyData, out columnNames, out rowBasedData, false);
            //
            if (columnNames.Length < 2 || rowBasedData.Length == 0) return;
            //
            List<double> xValues = new List<double>();
            List<double> yValues = new List<double>();
            for (int col = 1; col < columnNames.Length; col++)
            {
                xValues.Clear();
                yValues.Clear();
                for (int row = 0; row < rowBasedData.Length; row++)
                {
                    if (rowBasedData[row][0] == null || rowBasedData[row][col] == null) continue;
                    xValues.Add(Convert.ToDouble(rowBasedData[row][0]));
                    yValues.Add(Convert.ToDouble(rowBasedData[row][col]));
                }
                if (yValues.Count == 0) continue;
                //
                plot.AddCurve(historyData.ComponentName + " - " + GetSingleLineColumnName(columnNames[col]),
                              xValues.ToArray(), yValues.ToArray());
            }
            //
            if (plot.XAxisTitle.Length == 0) plot.XAxisTitle = GetSingleLineColumnName(columnNames[0]);
        }
        private string GetSingleLineColumnName(string columnName)
        {
            if (columnName == null) return "";
            return columnName.Replace(Environment.NewLine, " ").Replace("\n", " ").Trim();
        }
    }
}
