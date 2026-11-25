// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;


namespace vtkControl
{
    // The look of the hover highlights, which the native view draws over the widgets
    public static class vtkMaxWidgetHighlight
    {
        // Variables
        public const double WidgetFillOpacity = 0.05;
        public const double WidgetBorderOpacity = 0.5;
        public const float WidgetBorderLineWidth = 2.0f;
        public const double SurfaceFillOpacity = 0.15;


        // Properties
        public static Color BorderColor { get { return Color.RoyalBlue; } }
        public static Color FillColor { get { return Color.LightSkyBlue; } }
    }
}
