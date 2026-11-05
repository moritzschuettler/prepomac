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
    // Mirrors vtkn_orientation of vtkNative.h; the names are those of pmx.view.set_orientation
    public enum VtkNativeOrientation
    {
        Front = 0,
        Back = 1,
        Top = 2,
        Bottom = 3,
        Left = 4,
        Right = 5,
        Isometric = 6,
        IsometricNegative = 7,
        Normal = 8,
        Vertical = 9
    }
}
