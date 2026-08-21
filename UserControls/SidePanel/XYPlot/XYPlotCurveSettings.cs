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
using System.Drawing;
using System.Drawing.Drawing2D;
using DynamicTypeDescriptor;

namespace UserControls
{
    public class XYPlotCurveSettings : IXYPlotSettings
    {
        // Variables                                                                                                                
        private static readonly float[] _lineWidths = new float[] { 0.5f, 1, 1.5f, 2, 2.5f, 3, 4, 5, 6, 8, 10 };
        protected DynamicCustomTypeDescriptor _dctd = null;
        private string _name;
        private Color _color;
        private float _lineWidth;
        private XYPlotLineStyleEnum _lineStyle;
        private bool _visible;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("The curve name shown in the plot legend and in the data grid column header.")]
        [Id(1, 1)]
        public string Name { get { return _name; } set { _name = value == null ? "" : value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Color")]
        [DescriptionAttribute("The color of the curve line.")]
        [Id(1, 2)]
        public Color Color { get { return _color; } set { _color = Color.FromArgb(255, value); } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(1, 10, "Line width")]
        [DescriptionAttribute("The width of the curve line in pixels.")]
        [Id(2, 2)]
        public float LineWidth
        {
            get { return _lineWidth; }
            set
            {
                if (value < 0.5f) _lineWidth = 0.5f;
                else if (value > 10) _lineWidth = 10;
                else _lineWidth = value;
            }
        }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(2, 10, "Line style")]
        [DescriptionAttribute("The style of the curve line.")]
        [Id(3, 2)]
        public XYPlotLineStyleEnum LineStyle { get { return _lineStyle; } set { _lineStyle = value; } }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(3, 10, "Visible")]
        [DescriptionAttribute("Draw the curve in the plot.")]
        [Id(4, 2)]
        public bool Visible { get { return _visible; } set { _visible = value; } }


        // Constructors                                                                                                             
        public XYPlotCurveSettings()
        {
            _name = "";
            _color = Color.Black;
            _lineWidth = 2;
            _lineStyle = XYPlotLineStyleEnum.Solid;
            _visible = true;
        }
        public XYPlotCurveSettings(XYPlotCurveSettings settings)
            : this()
        {
            CopyFrom(settings);
        }


        // Methods                                                                                                                  
        public void CopyFrom(XYPlotCurveSettings settings)
        {
            if (settings == null) return;
            //
            _name = settings.Name;
            _color = settings.Color;
            _lineWidth = settings.LineWidth;
            _lineStyle = settings.LineStyle;
            _visible = settings.Visible;
        }
        public DashStyle GetDashStyle()
        {
            switch (_lineStyle)
            {
                case XYPlotLineStyleEnum.Dash: return DashStyle.Dash;
                case XYPlotLineStyleEnum.Dot: return DashStyle.Dot;
                case XYPlotLineStyleEnum.DashDot: return DashStyle.DashDot;
                case XYPlotLineStyleEnum.DashDotDot: return DashStyle.DashDotDot;
                default: return DashStyle.Solid;
            }
        }
        public void SetDashStyle(DashStyle dashStyle)
        {
            switch (dashStyle)
            {
                case DashStyle.Dash: _lineStyle = XYPlotLineStyleEnum.Dash; break;
                case DashStyle.Dot: _lineStyle = XYPlotLineStyleEnum.Dot; break;
                case DashStyle.DashDot: _lineStyle = XYPlotLineStyleEnum.DashDot; break;
                case DashStyle.DashDotDot: _lineStyle = XYPlotLineStyleEnum.DashDotDot; break;
                default: _lineStyle = XYPlotLineStyleEnum.Solid; break;
            }
        }
        public void InstallProvider()
        {
            _dctd = ProviderInstaller.Install(this);
            //
            _dctd.RenameBooleanProperty(nameof(Visible), "Yes", "No");
            //
            XYPlotSettingsTools.PopulateNumericProperty(_dctd, nameof(LineWidth), _lineWidths);
        }
        public void UpdateVisibility()
        {
        }
    }
}
