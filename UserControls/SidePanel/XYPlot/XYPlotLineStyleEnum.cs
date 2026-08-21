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
    public enum XYPlotLineStyleEnum
    {
        [StandardValue("Solid", "A continuous line.")]
        Solid,
        //
        [StandardValue("Dash", "A line of dashes.")]
        Dash,
        //
        [StandardValue("Dot", "A line of dots.")]
        Dot,
        //
        [StandardValue("Dash dot", "A line of alternating dashes and dots.")]
        DashDot,
        //
        [StandardValue("Dash dot dot", "A line of dashes followed by two dots.")]
        DashDotDot
    }
}
