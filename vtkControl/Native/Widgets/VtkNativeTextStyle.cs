// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_text_style of vtkNative.h, field by field so that the struct stays blittable
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeTextStyle
    {
        // Variables
        public double ColorR;
        public double ColorG;
        public double ColorB;
        public double LineOffset;
        public double LineSpacing;
        public int FontSize;
        public int FontFamily;
        public int Bold;
        public int Italic;


        // Static methods
        // The text of every vtkControl widget: CreateNewTextProperty
        public static VtkNativeTextStyle CreateWidgetStyle()
        {
            VtkNativeTextStyle style = new VtkNativeTextStyle();
            style.FontFamily = (int)VtkNativeFontFamily.Arial;
            style.FontSize = 16;
            style.LineOffset = -Math.Round(style.FontSize / 7.0);
            style.LineSpacing = 1.5;
            return style;
        }
    }
}
