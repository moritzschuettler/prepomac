// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Drawing;
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_highlight_style of vtkNativeSelection.h, field by field so that the struct stays blittable: how a
    // highlight of the selection layer is drawn
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeHighlightStyle
    {
        // Variables
        public double ColorR;
        public double ColorG;
        public double ColorB;
        public double Ambient;
        public double Diffuse;
        public double Specular;
        public double PointSize;
        public double LineWidth;
        public int ElementEdges;
        public int Points;
        public int OnGeometry;
        public int Current;


        // Static methods
        // Globals.CurrentMouseSelectionProperty, which the PickBy* paths give the highlight of the mouse selection
        public static VtkNativeHighlightStyle CreateMouseSelectionStyle(Color color)
        {
            VtkNativeHighlightStyle style = new VtkNativeHighlightStyle();
            style.ColorR = color.R / 255d;
            style.ColorG = color.G / 255d;
            style.ColorB = color.B / 255d;
            style.Ambient = 0.5;
            style.Diffuse = 1;
            style.Specular = 0.2;
            style.PointSize = 7;
            style.LineWidth = 2;
            style.Current = 1;
            return style;
        }
    }
}
