// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_interaction_settings of vtkNativeWidgets.h: the mouse settings of the interaction style
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeInteractionSettings
    {
        // Variables
        public double RotateSensitivity;
        public double ZoomSensitivity;
        public int InvertZoom;
        public int SmoothZoom;
        public int SelectionBoxEnabled;
        public int SelectionDistance;
    }
}
