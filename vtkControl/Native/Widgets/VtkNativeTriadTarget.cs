// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace vtkControl.Native
{
    // Mirrors vtkn_triad_target of vtkNativeWidgets.h and vtkMaxCoorSysTarget, in the same order
    public enum VtkNativeTriadTarget
    {
        None = 0,
        Origin = 1,
        XPositive = 2,
        XNegative = 3,
        YPositive = 4,
        YNegative = 5,
        ZPositive = 6,
        ZNegative = 7,
        XPositiveVertical = 8,
        XNegativeVertical = 9,
        YPositiveVertical = 10,
        YNegativeVertical = 11,
        ZPositiveVertical = 12,
        ZNegativeVertical = 13
    }
}
