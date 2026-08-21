// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;

namespace UserControls
{
    [Serializable]
    public enum XYPlotLegendPositionEnum
    {
        [StandardValue("Top center", "Above the plot and centered.")]
        TopCenter,
        //
        [StandardValue("Top", "Above the plot and aligned with its right edge.")]
        Top,
        //
        [StandardValue("Top flush left", "Above the plot and flush with the left margin.")]
        TopFlushLeft,
        //
        [StandardValue("Bottom center", "Below the plot and centered.")]
        BottomCenter,
        //
        [StandardValue("Bottom", "Below the plot and aligned with its right edge.")]
        Bottom,
        //
        [StandardValue("Bottom flush left", "Below the plot and flush with the left margin.")]
        BottomFlushLeft,
        //
        [StandardValue("Left", "On the left side of the plot.")]
        Left,
        //
        [StandardValue("Right", "On the right side of the plot.")]
        Right,
        //
        [StandardValue("Inside top left", "Inside the plot in the top left corner.")]
        InsideTopLeft,
        //
        [StandardValue("Inside top right", "Inside the plot in the top right corner.")]
        InsideTopRight,
        //
        [StandardValue("Inside bottom left", "Inside the plot in the bottom left corner.")]
        InsideBotLeft,
        //
        [StandardValue("Inside bottom right", "Inside the plot in the bottom right corner.")]
        InsideBotRight
    }
}
