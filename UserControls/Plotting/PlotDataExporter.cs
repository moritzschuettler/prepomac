// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace UserControls
{
    public static class PlotDataExporter
    {
        public static string GetDelimitedText(PlotViewData plot, string delimiter)
        {
            StringBuilder text = new StringBuilder();
            using (StringWriter writer = new StringWriter(text, CultureInfo.InvariantCulture))
                WriteDelimited(writer, plot, delimiter);
            return text.ToString();
        }

        public static void WriteDelimited(TextWriter writer, PlotViewData plot, string delimiter)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (plot == null) plot = new PlotViewData();
            if (string.IsNullOrEmpty(delimiter)) delimiter = ",";

            writer.WriteLine(string.Join(delimiter, new string[]
            {
                Escape("Curve", delimiter),
                Escape("Point", delimiter),
                Escape(plot.XAxisTitle ?? "X", delimiter),
                Escape(plot.YAxisTitle ?? "Y", delimiter)
            }));

            PlotCurveViewData[] curves = plot.Curves ?? new PlotCurveViewData[0];
            foreach (PlotCurveViewData curve in curves)
            {
                if (curve == null) continue;
                double[] x = curve.XValues ?? new double[0];
                double[] y = curve.YValues ?? new double[0];
                int count = Math.Min(x.Length, y.Length);
                for (int i = 0; i < count; i++)
                {
                    writer.WriteLine(string.Join(delimiter, new string[]
                    {
                        Escape(curve.Name, delimiter),
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        x[i].ToString("G17", CultureInfo.InvariantCulture),
                        y[i].ToString("G17", CultureInfo.InvariantCulture)
                    }));
                }
            }
        }

        private static string Escape(string value, string delimiter)
        {
            value = value ?? "";
            string quoteText = ((char)34).ToString();
            bool quote = value.Contains(delimiter) || value.Contains(quoteText) ||
                         value.Contains("\r") || value.Contains("\n");
            if (value.Contains(quoteText)) value = value.Replace(quoteText, quoteText + quoteText);
            return quote ? quoteText + value + quoteText : value;
        }
    }
}
