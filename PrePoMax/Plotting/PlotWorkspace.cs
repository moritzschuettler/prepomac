// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;

namespace PrePoMax
{
    [Serializable]
    public class HistorySeriesSource
    {
        public string SetName { get; set; }
        public string FieldName { get; set; }
        public string ComponentName { get; set; }
        public string EntryName { get; set; }

        public HistorySeriesSource(string setName, string fieldName, string componentName,
                                   string entryName = null)
        {
            SetName = setName;
            FieldName = fieldName;
            ComponentName = componentName;
            EntryName = entryName;
        }

        public HistorySeriesSource DeepCopy()
        {
            return new HistorySeriesSource(SetName, FieldName, ComponentName, EntryName);
        }

        public override string ToString()
        {
            string path = SetName + "/" + FieldName + "/" + ComponentName;
            if (!string.IsNullOrEmpty(EntryName)) path += "/" + EntryName;
            return path;
        }
    }

    [Serializable]
    public class PlotCurveDefinition
    {
        public string Name { get; set; }
        public HistorySeriesSource XSource { get; set; }
        public HistorySeriesSource YSource { get; set; }
        public int ColorArgb { get; set; }
        public bool Visible { get; set; }

        public PlotCurveDefinition(string name, HistorySeriesSource xSource, HistorySeriesSource ySource, int colorArgb)
        {
            Name = name;
            XSource = xSource;
            YSource = ySource;
            ColorArgb = colorArgb;
            Visible = true;
        }
    }

    [Serializable]
    public class PlotDefinition
    {
        public string Title { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public List<PlotCurveDefinition> Curves { get; private set; }

        public PlotDefinition(string title, string xAxisTitle, string yAxisTitle)
        {
            Title = title;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
            Curves = new List<PlotCurveDefinition>();
        }
    }

    [Serializable]
    public class PlotWorkspace
    {
        public const int MaximumPlotCount = 4;

        public List<PlotDefinition> Plots { get; private set; }

        public PlotWorkspace()
        {
            Plots = new List<PlotDefinition>();
        }

        public void Clear()
        {
            Plots.Clear();
        }
    }
}
