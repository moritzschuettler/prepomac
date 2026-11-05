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
    // Mirrors vtkn_part_style of vtkNative.h: the drawing fields of vtkMaxActorData. The two colors are the
    // double[4] arrays of the C struct, written out field by field so that the struct stays blittable
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativePartStyle
    {
        // Variables
        public double ColorR;
        public double ColorG;
        public double ColorB;
        public double ColorA;
        public double BackfaceColorR;
        public double BackfaceColorG;
        public double BackfaceColorB;
        public double BackfaceColorA;
        public double Ambient;
        public double Diffuse;
        public double LineWidth;
        public double NodeSize;
        public int HasBackfaceColor;
        public int CanHaveElementEdges;
        public int BackfaceCulling;
        public int Lighting;
        public int UseGlobalLighting;
        public int UseEdgeVisibility;
        public int Pickable;
        public int Visible;


        // Static methods
        public static VtkNativePartStyle FromActorData(vtkMaxActorData data)
        {
            VtkNativePartStyle style = new VtkNativePartStyle();
            style.SetColor(data.Color);
            style.HasBackfaceColor = data.BackfaceColor.IsEmpty ? 0 : 1;
            if (!data.BackfaceColor.IsEmpty)
            {
                style.BackfaceColorR = data.BackfaceColor.R / 255d;
                style.BackfaceColorG = data.BackfaceColor.G / 255d;
                style.BackfaceColorB = data.BackfaceColor.B / 255d;
                style.BackfaceColorA = data.BackfaceColor.A / 255d;
            }
            style.Ambient = data.Ambient;
            style.Diffuse = data.Diffuse;
            style.LineWidth = data.LineWidth;
            style.NodeSize = data.NodeSize;
            style.CanHaveElementEdges = data.CanHaveElementEdges ? 1 : 0;
            style.BackfaceCulling = data.BackfaceCulling ? 1 : 0;
            style.Lighting = data.Lighting ? 1 : 0;
            style.UseGlobalLighting = data.UseGlobalLighting ? 1 : 0;
            style.UseEdgeVisibility = data.UseEdgeVisibility ? 1 : 0;
            style.Pickable = data.Pickable ? 1 : 0;
            style.Visible = 1;
            return style;
        }


        // Methods
        public void SetColor(Color color)
        {
            ColorR = color.R / 255d;
            ColorG = color.G / 255d;
            ColorB = color.B / 255d;
            ColorA = color.A / 255d;
        }
    }
}
