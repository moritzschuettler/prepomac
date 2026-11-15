// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace vtkControl
{
    // What the touchpad and the trackpad navigation moves: the native interaction style
    internal interface ITrackpadNavigationTarget
    {
        bool NavigationAllowed { get; }
        void StartMultiTouchGesture(int x, int y, bool showRotationCenter);
        void EndMultiTouchGesture();
        void ZoomAtDisplayPoint(double parallelScaleFactor, int x, int y, bool render);
        void PanByDisplayDelta(double dx, double dy);
        void RotateByDisplayDelta(double dx, double dy);
    }
}
