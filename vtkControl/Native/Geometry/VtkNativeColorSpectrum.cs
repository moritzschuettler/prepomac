// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_color_spectrum of vtkNativeGeometry.h: the fields of vtkMaxColorSpectrum the result coloring reads.
    // The two colors are the double[3] arrays of the C struct, written out field by field
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeColorSpectrum
    {
        // Variables
        public int Type;
        public int Mode;
        public int MinMaxType;
        public int NumberOfColors;
        public int ReverseColors;
        public int Reserved;
        public double ColorBrightness;
        public double MinUserValue;
        public double MaxUserValue;
        public double MinColorR;
        public double MinColorG;
        public double MinColorB;
        public double MaxColorR;
        public double MaxColorG;
        public double MaxColorB;


        // Static methods
        public static VtkNativeColorSpectrum FromColorSpectrum(vtkMaxColorSpectrum colorSpectrum)
        {
            VtkNativeColorSpectrum spectrum = new VtkNativeColorSpectrum();
            spectrum.Type = (int)colorSpectrum.Type;
            spectrum.Mode = (int)colorSpectrum.Mode;
            spectrum.MinMaxType = (int)colorSpectrum.MinMaxType;
            spectrum.NumberOfColors = colorSpectrum.NumberOfColors;
            spectrum.ReverseColors = colorSpectrum.ReverseColors ? 1 : 0;
            spectrum.ColorBrightness = colorSpectrum.ColorBrightness;
            spectrum.MinUserValue = colorSpectrum.MinUserValue;
            spectrum.MaxUserValue = colorSpectrum.MaxUserValue;
            spectrum.MinColorR = colorSpectrum.MinColor.R / 255.0;
            spectrum.MinColorG = colorSpectrum.MinColor.G / 255.0;
            spectrum.MinColorB = colorSpectrum.MinColor.B / 255.0;
            spectrum.MaxColorR = colorSpectrum.MaxColor.R / 255.0;
            spectrum.MaxColorG = colorSpectrum.MaxColor.G / 255.0;
            spectrum.MaxColorB = colorSpectrum.MaxColor.B / 255.0;
            return spectrum;
        }


        // Methods
        // The RGB color of the spectrum at a position from 0 to 1
        public double[] GetColor(double position)
        {
            double[] rgb = new double[3];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_color_spectrum_get_colors(ref this, new double[] { position }, 1,
                                                                                   rgb));
            return rgb;
        }
    }
}
