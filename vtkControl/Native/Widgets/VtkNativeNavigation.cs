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
    // Mirrors vtkn_navigation of vtkNativeWidgets.h: the navigation calls of the interaction style
    public enum VtkNativeNavigation
    {
        RotateByDisplayDelta = 0,
        PanByDisplayDelta = 1,
        ZoomAtPoint = 2,
        ZoomAtPointSmooth = 3,
        Rotate = 4,
        StartGesture = 5,
        EndGesture = 6,
        FinishSmoothZoom = 7,
        StepSmoothZoom = 8,
        ResetRotationCenter = 9,
        FireSelectionTimer = 10,
        SetBoxZoom = 11,
        SetAnimating = 12
    }
}
