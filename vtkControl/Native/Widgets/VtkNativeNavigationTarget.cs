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
    // The native interaction style of a view as the target of the touchpad and the trackpad navigation
    internal sealed class VtkNativeNavigationTarget : ITrackpadNavigationTarget
    {
        // Variables
        private readonly VtkNativeView _view;


        // Properties
        public bool NavigationAllowed { get { return _view.GetInteractionState()[0] == 0; } }


        // Constructors
        public VtkNativeNavigationTarget(VtkNativeView view)
        {
            _view = view;
        }


        // Methods
        public void StartMultiTouchGesture(int x, int y, bool showRotationCenter)
        {
            _view.Navigate(VtkNativeNavigation.StartGesture, x, y, showRotationCenter ? 1 : 0);
        }
        public void EndMultiTouchGesture()
        {
            _view.Navigate(VtkNativeNavigation.EndGesture);
        }
        public void ZoomAtDisplayPoint(double parallelScaleFactor, int x, int y, bool render)
        {
            _view.Navigate(VtkNativeNavigation.ZoomAtPoint, parallelScaleFactor, x, y);
        }
        public void PanByDisplayDelta(double dx, double dy)
        {
            _view.Navigate(VtkNativeNavigation.PanByDisplayDelta, dx, dy);
        }
        public void RotateByDisplayDelta(double dx, double dy)
        {
            _view.Navigate(VtkNativeNavigation.RotateByDisplayDelta, dx, dy);
        }
    }
}
