// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeResults;
using System;
using System.Windows.Forms;
using UserControls;

namespace PrePoMax
{
    public partial class FrmMain
    {
        private PlotPanel _plotPanel;

        private void tsmiPlotPanel_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized) return;

                tsmiPlotPanel.Checked = !tsmiPlotPanel.Checked;
                if (tsmiPlotPanel.Checked)
                {
                    if (_plotPanel == null)
                    {
                        _plotPanel = new PlotPanel();
                        _plotPanel.CloseButtonPressedEvent += tsmiPlotPanel_Click;
                        _plotPanel.RemoveRequested += PlotPanel_RemoveRequested;
                        _plotPanel.DataRequested += PlotPanel_DataRequested;
                    }

                    UpdatePlotPanel();
                    ShowSidePanel(_plotPanel, 0.5, 100, 1000, false);
                }
                else HideSidePanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        public void UpdatePlotPanel()
        {
            if (_plotPanel == null || _controller == null) return;
            InvokeIfRequired(() => _plotPanel.SetPlots(_controller.GetPlotViewData()));
        }

        private void PlotPanel_RemoveRequested(object sender, PlotEventArgs e)
        {
            try
            {
                _controller.RemovePlotCommand(e.PlotIndex);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private bool CanAddHistoryOutputToPlot()
        {
            return _controller != null && _controller.PlotWorkspace.Plots.Count > 0;
        }

        private bool CanAddHistoryOutputToNewPlot()
        {
            return _controller != null &&
                   _controller.PlotWorkspace.Plots.Count < PlotWorkspace.MaximumPlotCount;
        }

        private int GetTargetPlotIndex()
        {
            int plotCount = _controller.PlotWorkspace.Plots.Count;
            if (plotCount == 0) return -1;
            int activeIndex = _plotPanel == null ? -1 : _plotPanel.ActivePlotIndex;
            if (activeIndex < 0 || activeIndex >= plotCount) activeIndex = plotCount - 1;
            return activeIndex;
        }

        private void ModelTree_AddHistoryOutputToPlot(HistoryResultData historyData)
        {
            try
            {
                int plotIndex = GetTargetPlotIndex();
                if (plotIndex < 0) throw new CaeException("Create a plot before using Add to Plot.");
                _controller.AddHistoryOutputToPlotCommand(plotIndex, historyData);
                ShowPlotPanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void ModelTree_AddHistoryOutputToNewPlot(HistoryResultData historyData)
        {
            try
            {
                _controller.AddHistoryOutputToNewPlotCommand(historyData);
                ShowPlotPanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void ShowPlotPanel()
        {
            if (!tsmiPlotPanel.Checked) tsmiPlotPanel_Click(this, EventArgs.Empty);
            else UpdatePlotPanel();
        }

        private void PlotPanel_DataRequested(object sender, PlotEventArgs e)
        {
            try
            {
                var plots = _controller.GetPlotViewData();
                if (e.PlotIndex < 0 || e.PlotIndex >= plots.Count) return;
                using (FrmPlotData form = new FrmPlotData())
                {
                    form.SetData(e.PlotIndex, plots[e.PlotIndex]);
                    form.ExportRequested += PlotData_ExportRequested;
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void PlotData_ExportRequested(object sender, PlotEventArgs e)
        {
            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    dialog.AddExtension = true;
                    dialog.DefaultExt = "csv";
                    dialog.FileName = "Plot_" + (e.PlotIndex + 1) + ".csv";
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        _controller.ExportPlotDataCommand(e.PlotIndex, dialog.FileName, ",");
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
    }
}
