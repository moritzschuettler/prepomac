// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using CaeResults;
using PrePoMax.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using UserControls;

namespace PrePoMax
{
    public partial class Controller
    {
        private const string EigenvalueHistoryComponentName = "EIGENVALUE_OUTPUT";
        private static readonly int[] PlotColors = new int[]
        {
            Color.FromArgb(31, 119, 180).ToArgb(),
            Color.FromArgb(214, 39, 40).ToArgb(),
            Color.FromArgb(44, 160, 44).ToArgb(),
            Color.FromArgb(255, 127, 14).ToArgb(),
            Color.FromArgb(148, 103, 189).ToArgb(),
            Color.FromArgb(23, 190, 207).ToArgb(),
            Color.FromArgb(140, 86, 75).ToArgb(),

            Color.FromArgb(227, 119, 194).ToArgb()
        };
        private sealed class HistoryEntryData
        {
            public FeResults Result;
            public HistoryResultField Field;
            public HistoryResultComponent Component;
            public HistoryResultEntries Entry;

            public HistoryEntryData(FeResults result, HistoryResultField field,
                                    HistoryResultComponent component, HistoryResultEntries entry)
            {
                Result = result; Field = field; Component = component; Entry = entry;
            }
        }
        [OptionalField] protected PlotWorkspace _plotWorkspace;
        public PlotWorkspace PlotWorkspace
        {
            get
            {
                if (_plotWorkspace == null) _plotWorkspace = new PlotWorkspace();
                return _plotWorkspace;
            }
        }
        public void ClearPlotWorkspace()
        {
            PlotWorkspace.Clear();
            if (_form != null) _form.UpdatePlotPanel();
        }
        private void LoadPlotWorkspace(Controller source)
        {
            _plotWorkspace = source == null || source._plotWorkspace == null
                                 ? new PlotWorkspace()
                                 : source._plotWorkspace;
            if (_form != null) _form.UpdatePlotPanel();
        }
        public void AddHistoryOutputToPlotCommand(int plotIndex, HistoryResultData historyData)
        {
            HistorySeriesSource source = CreateHistorySeriesSource(historyData);
            CAddHistoryOutputToPlot command = new CAddHistoryOutputToPlot(plotIndex, source);
            _commands.AddAndExecute(command);
        }
        public void AddHistoryOutputToNewPlotCommand(HistoryResultData historyData)
        {
            HistorySeriesSource source = CreateHistorySeriesSource(historyData);
            CAddHistoryOutputToNewPlot command = new CAddHistoryOutputToNewPlot(source);
            _commands.AddAndExecute(command);
        }
        public void CombineHistoryOutputsCommand(HistoryResultData xHistoryData, HistoryResultData yHistoryData)
        {
            HistorySeriesSource xSource = CreateHistorySeriesSource(xHistoryData);
            HistorySeriesSource ySource = CreateHistorySeriesSource(yHistoryData);
            string outputName = GetHistoryResultSetNames().GetNextNumberedKey("Combine");
            CCombineHistoryOutputs command = new CCombineHistoryOutputs(outputName, xSource, ySource);
            _commands.AddAndExecute(command);
        }
        public HistoryResultData[] GetResultHistoryComponents()
        {
            FeResults result = CurrentResult;
            return result == null ? new HistoryResultData[0] : result.GetHistoryResultComponents();
        }
        public void ExportPlotDataCommand(int plotIndex, string fileName, string delimiter)
        {
            CExportPlotDataToCsv command = new CExportPlotDataToCsv(plotIndex, fileName, delimiter);
            _commands.AddAndExecute(command);
        }
        public void RemovePlotCommand(int plotIndex)
        {
            CRemovePlot command = new CRemovePlot(plotIndex);
            _commands.AddAndExecute(command);
        }
        public void AddHistoryOutputToPlot(int plotIndex, HistorySeriesSource source)
        {
            if (plotIndex < 0 || plotIndex >= PlotWorkspace.Plots.Count)
                throw new CaeException("The target plot index is out of range.");

            AddHistoryCurves(PlotWorkspace.Plots[plotIndex], source);
            _form.UpdatePlotPanel();
        }
        public void AddHistoryOutputToNewPlot(HistorySeriesSource source)
        {
            if (PlotWorkspace.Plots.Count >= PlotWorkspace.MaximumPlotCount)
                throw new CaeException("The plot panel can contain at most four plots.");

            FeResults result;
            HistoryResultSet set;
            HistoryResultField field;
            HistoryResultComponent component;
            GetHistoryComponent(source, out result, out set, out field, out component);

            PlotDefinition plot = new PlotDefinition(GetDefaultPlotTitle(set, field, component),
                                                     GetDefaultXAxisTitle(result, set, component),
                                                     GetDefaultYAxisTitle(result, field, component, source.EntryName));
            AddHistoryCurves(plot, source);
            PlotWorkspace.Plots.Add(plot);
            _form.UpdatePlotPanel();
        }
        public void CombineHistoryOutputs(string outputName, HistorySeriesSource xSource,
                                          HistorySeriesSource ySource)
        {
            if (string.IsNullOrWhiteSpace(outputName))
                throw new CaeException("The XY history output name is not defined.");

            AddResultHistoryOutput(new ResultHistoryOutputXY(outputName, CreateHistoryResultData(xSource),
                                                             CreateHistoryResultData(ySource)));
        }
        public void RemovePlot(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= PlotWorkspace.Plots.Count)
                throw new CaeException("The plot index is out of range.");

            PlotWorkspace.Plots.RemoveAt(plotIndex);
            _form.UpdatePlotPanel();
        }
        public List<PlotViewData> GetPlotViewData()
        {
            List<PlotViewData> viewData = new List<PlotViewData>();
            foreach (PlotDefinition plot in PlotWorkspace.Plots)
            {
                PlotViewData viewPlot = new PlotViewData();
                viewPlot.Title = plot.Title;
                viewPlot.XAxisTitle = plot.XAxisTitle;
                viewPlot.YAxisTitle = plot.YAxisTitle;
                List<PlotCurveViewData> curves = new List<PlotCurveViewData>();

                foreach (PlotCurveDefinition curve in plot.Curves)
                    curves.Add(CreatePlotCurveViewData(curve));

                viewPlot.Curves = curves.ToArray();
                viewData.Add(viewPlot);
            }
            return viewData;
        }
        public void ExportPlotData(string fileName, int plotIndex, string delimiter)
        {
            if (plotIndex < 0 || plotIndex >= PlotWorkspace.Plots.Count)
                throw new CaeException("The plot index is out of range.");
            if (string.IsNullOrEmpty(delimiter)) delimiter = ",";

            PlotViewData plot = GetPlotViewData()[plotIndex];
            using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                PlotDataExporter.WriteDelimited(writer, plot, delimiter);
            _form.WriteDataToOutput("Plot data exported to file: " + fileName);
        }
        private HistorySeriesSource CreateHistorySeriesSource(HistoryResultData historyData)
        {
            if (historyData == null) throw new ArgumentNullException("historyData");
            if (CurrentResult == null)
                throw new CaeException("There is no current result for the selected history output.");

            return new HistorySeriesSource(historyData.SetName, historyData.FieldName,
                                           historyData.ComponentName);
        }
        private static HistoryResultData CreateHistoryResultData(HistorySeriesSource source)
        {
            if (source == null) throw new ArgumentNullException("source");

            return new HistoryResultData(source.SetName, source.FieldName, source.ComponentName);
        }
        private void AddHistoryCurves(PlotDefinition plot, HistorySeriesSource source)
        {
            FeResults result;
            HistoryResultSet set;
            HistoryResultField field;
            HistoryResultComponent component;
            GetHistoryComponent(source, out result, out set, out field, out component);

            IEnumerable<KeyValuePair<string, HistoryResultEntries>> entries = component.Entries;
            if (!string.IsNullOrEmpty(source.EntryName))
                entries = entries.Where(entry => string.Equals(entry.Key, source.EntryName,
                                                                StringComparison.OrdinalIgnoreCase));

            int added = 0;
            foreach (KeyValuePair<string, HistoryResultEntries> entry in entries)
            {
                HistorySeriesSource ySource = source.DeepCopy();
                ySource.EntryName = entry.Key;
                string curveName = source.SetName + ": " + entry.Key;
                int color = PlotColors[plot.Curves.Count % PlotColors.Length];
                plot.Curves.Add(new PlotCurveDefinition(curveName, null, ySource, color));
                added++;
            }

            if (added == 0)
                throw new CaeException("The selected history output does not contain any plottable entries.");
        }
        private void GetHistoryComponent(HistorySeriesSource source, out FeResults result, out HistoryResultSet set,
                                         out HistoryResultField field, out HistoryResultComponent component)
        {
            if (source == null) throw new ArgumentNullException("source");
            result = CurrentResult;
            if (result == null)
                throw new CaeException("There is no current result for the plot.");

            set = result.GetHistoryResultSet(source.SetName);
            field = result.GetHistoryResultField(source.SetName, source.FieldName);
            component = result.GetHistoryResultComponent(source.SetName, source.FieldName, source.ComponentName);
            if (set == null || field == null || component == null)
                throw new CaeException("The history output '" + source + "' cannot be found.");
        }
        private PlotCurveViewData CreatePlotCurveViewData(PlotCurveDefinition curve)
        {
            PlotCurveViewData viewCurve = new PlotCurveViewData();
            viewCurve.Name = curve.Name;
            viewCurve.Color = Color.FromArgb(curve.ColorArgb);
            viewCurve.Visible = curve.Visible;

            HistoryResultEntries yEntry = GetHistoryEntryOrNull(curve.YSource);
            if (yEntry == null)
            {
                viewCurve.Name += " (missing)";
                return viewCurve;
            }

            if (curve.XSource == null)
            {
                int count = Math.Min(yEntry.Time.Count, yEntry.Values.Count);
                viewCurve.XValues = yEntry.Time.Take(count).ToArray();
                viewCurve.YValues = yEntry.Values.Take(count).ToArray();
            }
            else
            {
                HistoryResultEntries xEntry = GetHistoryEntryOrNull(curve.XSource);
                if (xEntry == null)
                {
                    viewCurve.Name += " (missing X data)";
                    return viewCurve;
                }

                Dictionary<double, double> xByTime = new Dictionary<double, double>();
                int xCount = Math.Min(xEntry.Time.Count, xEntry.Values.Count);
                for (int i = 0; i < xCount; i++) xByTime[xEntry.Time[i]] = xEntry.Values[i];

                List<double> xValues = new List<double>();
                List<double> yValues = new List<double>();
                int yCount = Math.Min(yEntry.Time.Count, yEntry.Values.Count);
                for (int i = 0; i < yCount; i++)
                {
                    double x;
                    if (xByTime.TryGetValue(yEntry.Time[i], out x))
                    {
                        xValues.Add(x);
                        yValues.Add(yEntry.Values[i]);
                    }
                }
                viewCurve.XValues = xValues.ToArray();
                viewCurve.YValues = yValues.ToArray();
            }

            return viewCurve;
        }
        private HistoryResultEntries GetHistoryEntryOrNull(HistorySeriesSource source)
        {
            if (source == null) return null;
            FeResults result = CurrentResult;
            if (result == null) return null;
            return result.GetHistoryResultEntry(source.SetName, source.FieldName, source.ComponentName, source.EntryName);
        }
        private string GetDefaultPlotTitle(HistoryResultSet set, HistoryResultField field,
                                           HistoryResultComponent component)
        {
            return set.Name + " - " + field.Name + " - " + component.Name;
        }
        private string GetDefaultXAxisTitle(FeResults result, HistoryResultSet set, HistoryResultComponent component)
        {
            string xAxisUnit = result.GetHistoryXAxisUnit(result.GetResultHistoryOutputOrNull(set.Name));
            if (xAxisUnit != null) return GetAxisTitle(ResultHistoryOutputFromTable.XAxisName, xAxisUnit);
            if (component.Name == EigenvalueHistoryComponentName) return "Mode";
            if (set.Harmonic) return "Angle [" + StringAngleDegConverter.GetUnitAbbreviation() + "]";
            return "Time [" + result.GetHistoryUnitAbbreviation("Time", null, -1, -1) + "]";
        }
        private static string GetAxisTitle(string name, string unit)
        {
            return name + (string.IsNullOrEmpty(unit) ? "" : " [" + unit + "]");
        }
        private string GetDefaultYAxisTitle(FeResults result, HistoryResultField field,
                                            HistoryResultComponent component, string entryName)
        {
            string unit = component.Unit;
            if (!string.IsNullOrEmpty(entryName) && component.Entries.ContainsKey(entryName) &&
                component.Entries[entryName].Unit != null)
                unit = component.Entries[entryName].Unit;
            if (unit == null) unit = result.GetHistoryUnitAbbreviation(field.Name, component.Name, -1, -1);
            return component.Name + (string.IsNullOrEmpty(unit) ? "" : " [" + unit + "]");
        }
    }
}
