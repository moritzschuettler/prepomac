// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeModel;
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
        private bool CanAddItemsToNewPlot()
        {
            return _xyPlotPanel == null || _xyPlotPanel.PlotCount < _xyPlotPanel.MaxPlotCount;
        }
        private void AddItemsToPlot(NamedClass[] items)
        {
            try
            {
                ShowXYPlotPanel();
                //
                XYPlot plot = _xyPlotPanel.SelectedPlot;
                if (plot == null) plot = _xyPlotPanel.AddPlot();
                //
                AddItemsToPlot(items, plot);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void AddItemsToNewPlot(NamedClass[] items)
        {
            try
            {
                ShowXYPlotPanel();
                //
                XYPlot plot = _xyPlotPanel.AddPlot();
                if (plot == null) return;
                //
                AddItemsToPlot(items, plot);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void AddItemsToPlot(NamedClass[] items, XYPlot plot)
        {
            if (plot == null || items == null) return;
            //
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is HistoryResultData historyData) AddHistoryOutputToPlot(historyData, plot);
                else if (items[i] is Amplitude amplitude) AddAmplitudeToPlot(amplitude, plot);
            }
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
                plot.AddCurve(GetCurveName(historyData, columnNames[col]), xValues.ToArray(), yValues.ToArray());
            }
            //
            if (plot.XAxisTitle.Length == 0) plot.XAxisTitle = GetSingleLineColumnName(columnNames[0]);
        }
        private void AddAmplitudeToPlot(Amplitude amplitude, XYPlot plot)
        {
            Amplitude modelAmplitude = _controller.GetAmplitude(amplitude.Name);
            if (modelAmplitude == null) return;
            //
            double[][] timeAmplitude = modelAmplitude.TimeAmplitude;
            if (timeAmplitude == null || timeAmplitude.Length == 0) return;
            // CalculiX shifts the amplitude data by the ShiftX and the ShiftY values
            double[] xValues = new double[timeAmplitude.Length];
            double[] yValues = new double[timeAmplitude.Length];
            for (int i = 0; i < timeAmplitude.Length; i++)
            {
                xValues[i] = timeAmplitude[i][0] + modelAmplitude.ShiftX;
                yValues[i] = timeAmplitude[i][1] + modelAmplitude.ShiftY;
            }
            //
            plot.AddCurve(modelAmplitude.Name, xValues, yValues);
            //
            if (plot.XAxisTitle.Length == 0)
                plot.XAxisTitle = "Time [" + _controller.Model.UnitSystem.TimeUnitAbbreviation + "]";
        }
        private string GetCurveName(HistoryResultData historyData, string columnName)
        {
            // The first row of the column name is the entry name, the rows that follow are the unit and the locality
            string[] rows = GetColumnNameRows(columnName);
            if (rows.Length == 0) return historyData.ComponentName;
            //
            string curveName = historyData.ComponentName + " - " + rows[0];
            for (int i = 1; i < rows.Length; i++) curveName += Environment.NewLine + rows[i];
            //
            return curveName;
        }
        private string[] GetColumnNameRows(string columnName)
        {
            if (columnName == null) return new string[0];
            //
            string[] rows = columnName.Replace(Environment.NewLine, "\n").Split('\n');
            List<string> rowsToKeep = new List<string>();
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Trim().Length > 0) rowsToKeep.Add(rows[i].Trim());
            }
            return rowsToKeep.ToArray();
        }
        private string GetSingleLineColumnName(string columnName)
        {
            if (columnName == null) return "";
            return columnName.Replace(Environment.NewLine, " ").Replace("\n", " ").Trim();
        }
    }
}
