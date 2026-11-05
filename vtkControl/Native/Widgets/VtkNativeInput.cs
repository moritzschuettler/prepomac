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
    // Mirrors vtkn_input_event of vtkNativeWidgets.h: the input of the interaction style of a view
    public enum VtkNativeInput
    {
        LeftButtonDown = 0,
        LeftButtonUp = 1,
        MiddleButtonDown = 2,
        MiddleButtonUp = 3,
        RightButtonDown = 4,
        RightButtonUp = 5,
        MouseMove = 6,
        WheelForward = 7,
        WheelBackward = 8,
        KeyPress = 9,
        KeyRelease = 10,
        Leave = 11
    }
}
