// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace vtkControl.Native
{
    // The areas of vtkControl the port takes over one by one; vtkControl sends an area that is on to its native view
    [Flags]
    public enum VtkNativeArea
    {
        None = 0,
        Geometry = 1,
        Widgets = 2,
        Selection = 4,
        All = Geometry | Widgets | Selection
    }
}
