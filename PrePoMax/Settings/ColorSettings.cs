// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using DynamicTypeDescriptor;
using System.Drawing;
using System.Runtime.Serialization;

namespace PrePoMax
{
    [Serializable]
    public enum ColorPaletteEnum
    {
        [StandardValue("Default", Description = "The default PrePoMax part colors.")]
        Default,

        [StandardValue("Expevo", Description = "Saturated warm to cool contrasts closing with gray.")]
        Expevo,

        [StandardValue("Minou", Description = "Teal, red and amber with muted blue gray tones.")]
        Minou,

        [StandardValue("Reading", Description = "Soft amber, sage and slate tones of low saturation.")]
        Reading,

        [StandardValue("Alger", Description = "Black and deep teal against sand and brick red.")]
        Alger,

        [StandardValue("Heatmap 0", Description = "A sequential ramp from dark blue over sand to deep red.")]
        Heatmap0,

        [StandardValue("Pantone 23", Description = "Steel blue and navy with amber, wine and olive.")]
        Pantone23,

        [StandardValue("Lincoln", Description = "Warm paper tones against dark navy and burnt orange.")]
        Lincoln,

        [StandardValue("Seafarer", Description = "Deep sea blue, cream, mist green and warm ochre.")]
        Seafarer,

        [StandardValue("Paloma", Description = "Light green, sand and peach closing with pink.")]
        Paloma,

        [StandardValue("Ploen", Description = "Dusty blue and lilac with apricot and rose.")]
        Ploen,

        [StandardValue("Gaby", Description = "Pale cream and coral shifting to blue and plum.")]
        Gaby,

        [StandardValue("Custom", Description = "The colors were changed individually.")]
        Custom,
    }

    [Serializable]
    public class ColorSettings : ISettings, Settings.IReset
    {
        // Variables                                                                                                                
        public const int NumOfColors = 10;
        public const double MinSaturation = 10;
        public const double MaxSaturation = 200;
        public const double DefaultSaturation = 100;
        private const double BrightnessCompensation = 0.6;
        //
        private ColorPaletteEnum _colorPalette;
        private double _saturation;
        private Color[] _colorTable;
        private Color _frontFaceColor;
        private Color _backFaceColor;
        private Color _transfiniteMeshColor;
        private Color _sweepMeshColor;
        private Color _tetrahedralMeshColor;
        private Color _otherMeshColor;

        // Properties                                                                                                               
        public ColorPaletteEnum ColorPalette
        {
            get { return _colorPalette; }
            set { _colorPalette = value; }
        }
        public double Saturation
        {
            get { return LimitSaturation(_saturation); }
            set { _saturation = LimitSaturation(value); }
        }
        public Color[] ColorTable
        {
            get { return _colorTable; }
            set { _colorTable = value; }
        }
        public Color FrontFaceColor
        {
            get { return _frontFaceColor; }
            set { _frontFaceColor = value; }
        }
        public Color BackFaceColor
        {
            get { return _backFaceColor; }
            set { _backFaceColor = value; }
        }
        public Color TransfiniteMeshColor
        {
            get { return _transfiniteMeshColor; }
            set { _transfiniteMeshColor = value; }
        }
        public Color SweepMeshColor
        {
            get { return _sweepMeshColor; }
            set { _sweepMeshColor = value; }
        }
        public Color TetrahedralMeshColor
        {
            get { return _tetrahedralMeshColor; }
            set { _tetrahedralMeshColor = value; }
        }
        public Color OtherMeshColor
        {
            get { return _otherMeshColor; }
            set { _otherMeshColor = value; }
        }


        // Constructors                                                                                                             
        public ColorSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
            Saturation = _saturation;
        }
        public void Reset()
        {
            _colorPalette = ColorPaletteEnum.Default;
            _saturation = DefaultSaturation;
            ApplyColorPalette();
            //
            _frontFaceColor = Color.Coral;
            _backFaceColor = Color.PowderBlue;
            //
            _transfiniteMeshColor = Color.DarkSeaGreen;
            _sweepMeshColor = Color.Khaki;
            _tetrahedralMeshColor = Color.Pink;
            _otherMeshColor = Color.LightGray;
        }
        public void ApplyColorPalette()
        {
            if (_colorPalette == ColorPaletteEnum.Custom) return;
            //
            _colorTable = SaturateColors(ResampleColors(GetPaletteColors(_colorPalette), NumOfColors), _saturation);
        }
        public void SetTableColor(int colorId, Color color)
        {
            _colorTable[colorId] = color;
            _colorPalette = ColorPaletteEnum.Custom;
        }
        private static Color[] GetPaletteColors(ColorPaletteEnum colorPalette)
        {
            switch (colorPalette)
            {
                case ColorPaletteEnum.Expevo:
                    return FromHtml("#FC4E07", "#E7B800", "#00AFBB", "#8B4769", "#1D457F", "#808080");
                case ColorPaletteEnum.Minou:
                    return FromHtml("#00798C", "#D1495B", "#EDAE49", "#66A182", "#2E4057", "#8D96A3");
                case ColorPaletteEnum.Reading:
                    return FromHtml("#EFBC68", "#919F89", "#EDBDAE", "#57717C", "#5F97A4", "#CAEAC8", "#95A1AE",
                                    "#C8CFD6");
                case ColorPaletteEnum.Alger:
                    return FromHtml("#000000", "#1A5B5B", "#ACC8BE", "#F4AB5C", "#D1422F");
                case ColorPaletteEnum.Heatmap0:
                    return FromHtml("#001219", "#005F73", "#0A9396", "#94D2BD", "#E9D8A6", "#EE9B00", "#CA6702",
                                    "#AE2012", "#9B2226");
                case ColorPaletteEnum.Pantone23:
                    return FromHtml("#7A92A5", "#1F2C43", "#FFB000", "#842C48", "#46483D");
                case ColorPaletteEnum.Lincoln:
                    return FromHtml("#EEE9DF", "#C9C1B1", "#2C3B4D", "#FFB162", "#A35139", "#1B2632");
                case ColorPaletteEnum.Seafarer:
                    return FromHtml("#013D5A", "#FCF3E3", "#BDD3CE", "#708C69", "#E4A25B");
                case ColorPaletteEnum.Paloma:
                    return FromHtml("#83AF9B", "#C8C8A9", "#F8DA8A", "#F7BF95", "#FE8CA1");
                case ColorPaletteEnum.Ploen:
                    return FromHtml("#3F5671", "#83A1C3", "#CEB5C8", "#FAC898", "#B17776");
                case ColorPaletteEnum.Gaby:
                    return FromHtml("#FCEAAB", "#F1A890", "#A8C4CC", "#82A0C2", "#85496F");
                default:
                    return new Color[]
                    {
                        Color.Beige,
                        Color.DarkSeaGreen,
                        Color.Khaki,
                        Color.SteelBlue,
                        Color.BurlyWood,
                        Color.LightSteelBlue,
                        Color.MistyRose,
                        Color.DarkSalmon,
                        Color.DarkKhaki,
                        Color.NavajoWhite
                    };
            }
        }
        private static Color[] FromHtml(params string[] htmlColors)
        {
            Color[] colors = new Color[htmlColors.Length];
            for (int i = 0; i < htmlColors.Length; i++) colors[i] = ColorTranslator.FromHtml(htmlColors[i]);
            return colors;
        }
        private static Color[] ResampleColors(Color[] colors, int numOfColors)
        {
            if (colors.Length == numOfColors) return colors;
            //
            Color[] resampledColors = new Color[numOfColors];
            double position;
            double ratio;
            int previousId;
            int nextId;
            //
            for (int i = 0; i < numOfColors; i++)
            {
                if (numOfColors > 1) position = (double)i * (colors.Length - 1) / (numOfColors - 1);
                else position = 0;
                //
                previousId = (int)Math.Floor(position);
                if (previousId > colors.Length - 2) previousId = Math.Max(colors.Length - 2, 0);
                nextId = Math.Min(previousId + 1, colors.Length - 1);
                ratio = position - previousId;
                //
                resampledColors[i] = Color.FromArgb(BlendValues(colors[previousId].R, colors[nextId].R, ratio),
                                                    BlendValues(colors[previousId].G, colors[nextId].G, ratio),
                                                    BlendValues(colors[previousId].B, colors[nextId].B, ratio));
            }
            return resampledColors;
        }
        private static int BlendValues(int previousValue, int nextValue, double ratio)
        {
            return (int)Math.Round(previousValue + ratio * (nextValue - previousValue), MidpointRounding.AwayFromZero);
        }
        private static Color[] SaturateColors(Color[] colors, double saturation)
        {
            if (saturation == DefaultSaturation) return colors;
            //
            double factor = saturation / DefaultSaturation;
            Color[] saturatedColors = new Color[colors.Length];
            double gray;
            double brightness;
            //
            for (int i = 0; i < colors.Length; i++)
            {
                gray = 0.299 * colors[i].R + 0.587 * colors[i].G + 0.114 * colors[i].B;
                if (factor < 1) brightness = (1 - factor) * BrightnessCompensation * (255 - gray);
                else brightness = 0;
                //
                saturatedColors[i] = Color.FromArgb(LimitColorValue(gray + brightness + factor * (colors[i].R - gray)),
                                                    LimitColorValue(gray + brightness + factor * (colors[i].G - gray)),
                                                    LimitColorValue(gray + brightness + factor * (colors[i].B - gray)));
            }
            return saturatedColors;
        }
        private static int LimitColorValue(double value)
        {
            int colorValue = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (colorValue < 0) return 0;
            else if (colorValue > 255) return 255;
            else return colorValue;
        }
        private static double LimitSaturation(double value)
        {
            if (value < MinSaturation) return MinSaturation;
            else if (value > MaxSaturation) return MaxSaturation;
            else return value;
        }
    }
}
