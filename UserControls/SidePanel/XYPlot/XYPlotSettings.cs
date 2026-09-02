// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using ZedGraph;

namespace UserControls
{
    [System.Serializable]
    public class XYPlotSettings : IXYPlotSettings
    {
        // Variables
        public const string DefaultXAxisTitle = "X axis";
        public const string DefaultYAxisTitle = "Y axis";
        private static readonly float[] _fontSizes = new float[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24 };
        [System.NonSerialized] protected DynamicCustomTypeDescriptor _dctd = null;
        private string _xAxisTitle;
        private bool _xAxisAutoScale;
        private double _xAxisMin;
        private double _xAxisMax;
        private string _yAxisTitle;
        private bool _yAxisAutoScale;
        private double _yAxisMin;
        private double _yAxisMax;
        private bool _showLegend;
        private XYPlotLegendPositionEnum _legendPosition;
        private bool _showPointSymbols;
        private XYPlotFontFamilyEnum _fontFamily;
        private float _fontSize;


        // Properties
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(0, 10, "Title")]
        [DescriptionAttribute("The title shown below the X axis.")]
        [Id(1, 1)]
        public string XAxisTitle { get { return _xAxisTitle; } set { _xAxisTitle = value == null ? "" : value; } }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(1, 10, "Auto scale")]
        [DescriptionAttribute("Compute the X axis range from the curve data.")]
        [Id(2, 1)]
        public bool XAxisAutoScale
        {
            get { return _xAxisAutoScale; }
            set
            {
                _xAxisAutoScale = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(2, 10, "Minimum")]
        [DescriptionAttribute("The minimum value of the X axis.")]
        [Id(3, 1)]
        public double XAxisMin { get { return _xAxisMin; } set { _xAxisMin = value; } }
        //
        [CategoryAttribute("X axis")]
        [OrderedDisplayName(3, 10, "Maximum")]
        [DescriptionAttribute("The maximum value of the X axis.")]
        [Id(4, 1)]
        public double XAxisMax { get { return _xAxisMax; } set { _xAxisMax = value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(0, 10, "Title")]
        [DescriptionAttribute("The title shown left of the Y axis.")]
        [Id(1, 2)]
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value == null ? "" : value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(1, 10, "Auto scale")]
        [DescriptionAttribute("Compute the Y axis range from the curve data.")]
        [Id(2, 2)]
        public bool YAxisAutoScale
        {
            get { return _yAxisAutoScale; }
            set
            {
                _yAxisAutoScale = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(2, 10, "Minimum")]
        [DescriptionAttribute("The minimum value of the Y axis.")]
        [Id(3, 2)]
        public double YAxisMin { get { return _yAxisMin; } set { _yAxisMin = value; } }
        //
        [CategoryAttribute("Y axis")]
        [OrderedDisplayName(3, 10, "Maximum")]
        [DescriptionAttribute("The maximum value of the Y axis.")]
        [Id(4, 2)]
        public double YAxisMax { get { return _yAxisMax; } set { _yAxisMax = value; } }
        //
        [CategoryAttribute("Legend")]
        [OrderedDisplayName(0, 10, "Show legend")]
        [DescriptionAttribute("Show the curve names in the plot legend.")]
        [Id(1, 3)]
        public bool ShowLegend
        {
            get { return _showLegend; }
            set
            {
                _showLegend = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Legend")]
        [OrderedDisplayName(1, 10, "Position")]
        [DescriptionAttribute("The position of the legend inside or around the plot.")]
        [Id(2, 3)]
        public XYPlotLegendPositionEnum LegendPosition { get { return _legendPosition; } set { _legendPosition = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Show point symbols")]
        [DescriptionAttribute("Draw a symbol at each curve data point.")]
        [Id(1, 4)]
        public bool ShowPointSymbols { get { return _showPointSymbols; } set { _showPointSymbols = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(1, 10, "Font")]
        [DescriptionAttribute("The font of the legend, the axis titles and the axis labels.")]
        [Id(2, 4)]
        public XYPlotFontFamilyEnum FontFamily { get { return _fontFamily; } set { _fontFamily = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(2, 10, "Font size")]
        [DescriptionAttribute("The font size of the legend, the axis titles and the axis labels.")]
        [Id(3, 4)]
        public float FontSize
        {
            get { return _fontSize; }
            set
            {
                if (value < 6) _fontSize = 6;
                else if (value > 30) _fontSize = 30;
                else _fontSize = value;
            }
        }


        // Constructors
        public XYPlotSettings()
        {
            _xAxisTitle = DefaultXAxisTitle;
            _xAxisAutoScale = true;
            _xAxisMin = 0;
            _xAxisMax = 1;
            _yAxisTitle = DefaultYAxisTitle;
            _yAxisAutoScale = true;
            _yAxisMin = 0;
            _yAxisMax = 1;
            _showLegend = true;
            _legendPosition = XYPlotLegendPositionEnum.TopCenter;
            _showPointSymbols = false;
            _fontFamily = XYPlotFontFamilyEnum.SegoeUI;
            _fontSize = 16;
        }
        public XYPlotSettings(XYPlotSettings settings)
            : this()
        {
            CopyFrom(settings);
        }


        // Methods
        public void CopyFrom(XYPlotSettings settings)
        {
            if (settings == null) return;
            //
            _xAxisTitle = settings.XAxisTitle;
            _xAxisAutoScale = settings.XAxisAutoScale;
            _xAxisMin = settings.XAxisMin;
            _xAxisMax = settings.XAxisMax;
            _yAxisTitle = settings.YAxisTitle;
            _yAxisAutoScale = settings.YAxisAutoScale;
            _yAxisMin = settings.YAxisMin;
            _yAxisMax = settings.YAxisMax;
            _showLegend = settings.ShowLegend;
            _legendPosition = settings.LegendPosition;
            _showPointSymbols = settings.ShowPointSymbols;
            _fontFamily = settings.FontFamily;
            _fontSize = settings.FontSize;
        }
        public string GetFontFamilyName()
        {
            switch (_fontFamily)
            {
                case XYPlotFontFamilyEnum.Arial: return "Arial";
                case XYPlotFontFamilyEnum.Calibri: return "Calibri";
                case XYPlotFontFamilyEnum.Verdana: return "Verdana";
                case XYPlotFontFamilyEnum.Tahoma: return "Tahoma";
                case XYPlotFontFamilyEnum.TimesNewRoman: return "Times New Roman";
                case XYPlotFontFamilyEnum.Cambria: return "Cambria";
                case XYPlotFontFamilyEnum.Consolas: return "Consolas";
                default: return "Segoe UI";
            }
        }
        public void SetFontFamilyName(string fontFamilyName)
        {
            switch (fontFamilyName)
            {
                case "Arial": _fontFamily = XYPlotFontFamilyEnum.Arial; break;
                case "Calibri": _fontFamily = XYPlotFontFamilyEnum.Calibri; break;
                case "Verdana": _fontFamily = XYPlotFontFamilyEnum.Verdana; break;
                case "Tahoma": _fontFamily = XYPlotFontFamilyEnum.Tahoma; break;
                case "Times New Roman": _fontFamily = XYPlotFontFamilyEnum.TimesNewRoman; break;
                case "Cambria": _fontFamily = XYPlotFontFamilyEnum.Cambria; break;
                case "Consolas": _fontFamily = XYPlotFontFamilyEnum.Consolas; break;
                default: _fontFamily = XYPlotFontFamilyEnum.SegoeUI; break;
            }
        }
        public LegendPos GetLegendPos()
        {
            switch (_legendPosition)
            {
                case XYPlotLegendPositionEnum.Top: return LegendPos.Top;
                case XYPlotLegendPositionEnum.TopFlushLeft: return LegendPos.TopFlushLeft;
                case XYPlotLegendPositionEnum.BottomCenter: return LegendPos.BottomCenter;
                case XYPlotLegendPositionEnum.Bottom: return LegendPos.Bottom;
                case XYPlotLegendPositionEnum.BottomFlushLeft: return LegendPos.BottomFlushLeft;
                case XYPlotLegendPositionEnum.Left: return LegendPos.Left;
                case XYPlotLegendPositionEnum.Right: return LegendPos.Right;
                case XYPlotLegendPositionEnum.InsideTopLeft: return LegendPos.InsideTopLeft;
                case XYPlotLegendPositionEnum.InsideTopRight: return LegendPos.InsideTopRight;
                case XYPlotLegendPositionEnum.InsideBotLeft: return LegendPos.InsideBotLeft;
                case XYPlotLegendPositionEnum.InsideBotRight: return LegendPos.InsideBotRight;
                default: return LegendPos.TopCenter;
            }
        }
        public void SetLegendPos(LegendPos legendPos)
        {
            switch (legendPos)
            {
                case LegendPos.Top: _legendPosition = XYPlotLegendPositionEnum.Top; break;
                case LegendPos.TopFlushLeft: _legendPosition = XYPlotLegendPositionEnum.TopFlushLeft; break;
                case LegendPos.BottomCenter: _legendPosition = XYPlotLegendPositionEnum.BottomCenter; break;
                case LegendPos.Bottom: _legendPosition = XYPlotLegendPositionEnum.Bottom; break;
                case LegendPos.BottomFlushLeft: _legendPosition = XYPlotLegendPositionEnum.BottomFlushLeft; break;
                case LegendPos.Left: _legendPosition = XYPlotLegendPositionEnum.Left; break;
                case LegendPos.Right: _legendPosition = XYPlotLegendPositionEnum.Right; break;
                case LegendPos.InsideTopLeft: _legendPosition = XYPlotLegendPositionEnum.InsideTopLeft; break;
                case LegendPos.InsideTopRight: _legendPosition = XYPlotLegendPositionEnum.InsideTopRight; break;
                case LegendPos.InsideBotLeft: _legendPosition = XYPlotLegendPositionEnum.InsideBotLeft; break;
                case LegendPos.InsideBotRight: _legendPosition = XYPlotLegendPositionEnum.InsideBotRight; break;
                default: _legendPosition = XYPlotLegendPositionEnum.TopCenter; break;
            }
        }
        public void InstallProvider()
        {
            _dctd = ProviderInstaller.Install(this);
            //
            _dctd.RenameBooleanProperty(nameof(XAxisAutoScale), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(YAxisAutoScale), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(ShowLegend), "Yes", "No");
            _dctd.RenameBooleanProperty(nameof(ShowPointSymbols), "Yes", "No");
            //
            XYPlotSettingsTools.PopulateNumericProperty(_dctd, nameof(FontSize), _fontSizes);
        }
        public void UpdateVisibility()
        {
            if (_dctd == null) return;
            //
            _dctd.GetProperty(nameof(XAxisMin)).SetIsBrowsable(!_xAxisAutoScale);
            _dctd.GetProperty(nameof(XAxisMax)).SetIsBrowsable(!_xAxisAutoScale);
            _dctd.GetProperty(nameof(YAxisMin)).SetIsBrowsable(!_yAxisAutoScale);
            _dctd.GetProperty(nameof(YAxisMax)).SetIsBrowsable(!_yAxisAutoScale);
            //
            _dctd.GetProperty(nameof(LegendPosition)).SetIsBrowsable(_showLegend);
        }
    }
}
