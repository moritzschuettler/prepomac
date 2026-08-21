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
    public enum XYPlotFontFamilyEnum
    {
        [StandardValue("Segoe UI", "The user interface font of PrePoMax.")]
        SegoeUI,
        //
        [StandardValue("Arial", "A widely available sans serif font.")]
        Arial,
        //
        [StandardValue("Calibri", "A rounded sans serif font.")]
        Calibri,
        //
        [StandardValue("Verdana", "A wide sans serif font that stays readable when small.")]
        Verdana,
        //
        [StandardValue("Tahoma", "A narrow sans serif font.")]
        Tahoma,
        //
        [StandardValue("Times New Roman", "A serif font used in printed reports.")]
        TimesNewRoman,
        //
        [StandardValue("Cambria", "A serif font designed for on screen reading.")]
        Cambria,
        //
        [StandardValue("Consolas", "A monospaced font with equally wide digits.")]
        Consolas
    }
}
