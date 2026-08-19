// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Drawing;

namespace UserControls
{
    public class PlotCurveViewData
    {
        public string Name { get; set; }
        public double[] XValues { get; set; }
        public double[] YValues { get; set; }
        public Color Color { get; set; }
        public bool Visible { get; set; }

        public PlotCurveViewData()
        {
            Name = "Curve";
            XValues = new double[0];
            YValues = new double[0];
            Color = Color.Blue;
            Visible = true;
        }
    }

    public class PlotViewData
    {
        public string Title { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }
        public PlotCurveViewData[] Curves { get; set; }

        public PlotViewData()
        {
            Title = "Plot";
            XAxisTitle = "X";
            YAxisTitle = "Y";
            Curves = new PlotCurveViewData[0];
        }
    }

    public class PlotEventArgs : EventArgs
    {
        public int PlotIndex { get; private set; }

        public PlotEventArgs(int plotIndex)
        {
            PlotIndex = plotIndex;
        }
    }
}
