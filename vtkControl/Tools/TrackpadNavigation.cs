// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;


namespace vtkControl
{
    public class TrackpadNavigation
    {
        // Variables
        public const double WheelDeltaPerNotch = 120.0;
        private const double _pixelsPerNotch = 40.0;
        private const double _zoomPerNotch = 1.15;
        private const double _pixelsPerMm = 10.0;
        //
        private bool _enabled;
        private bool _useWheelFallback;
        private bool _twoFingerDragPans;
        private bool _invertX;
        private bool _invertY;
        private double _panSensitivity;


        // Properties
        public bool Enabled { get { return _enabled; } set { _enabled = value; } }
        public bool UseWheelFallback { get { return _useWheelFallback; } set { _useWheelFallback = value; } }
        public bool TwoFingerDragPans { get { return _twoFingerDragPans; } set { _twoFingerDragPans = value; } }
        public bool InvertX { get { return _invertX; } set { _invertX = value; } }
        public bool InvertY { get { return _invertY; } set { _invertY = value; } }
        public double PanSensitivity { get { return _panSensitivity; } set { _panSensitivity = value; } }


        // Constructors
        public TrackpadNavigation()
        {
            _enabled = true;
            _useWheelFallback = false;
            _twoFingerDragPans = false;
            _invertX = false;
            _invertY = false;
            _panSensitivity = 1;
        }


        // Methods
        public static double MmToPixels(double mm)
        {
            return mm * _pixelsPerMm;
        }
        public static double WheelDeltaToPixels(double wheelDelta)
        {
            return wheelDelta / WheelDeltaPerNotch * _pixelsPerNotch;
        }
        public static double WheelDeltaToZoomFactor(double wheelDelta)
        {
            return Math.Pow(_zoomPerNotch, wheelDelta / WheelDeltaPerNotch);
        }
        internal void Apply(vtkInteractorStyleControl style, double dragPixelsX, double dragPixelsY,
                          double zoomFactor, int zoomCenterX, int zoomCenterY, bool shiftPressed)
        {
            if (style == null || !style.NavigationAllowed) return;
            //
            if (_invertX) dragPixelsX = -dragPixelsX;
            if (_invertY) dragPixelsY = -dragPixelsY;
            //
            bool zoom = zoomFactor != 1;
            bool drag = dragPixelsX != 0 || dragPixelsY != 0;
            if (!zoom && !drag) return;
            //
            bool pan = shiftPressed ^ _twoFingerDragPans;
            style.StartMultiTouchGesture(zoomCenterX, zoomCenterY, drag && !pan);
            //
            // The rotation and the zoom carry the sensitivity that is shared with the mouse
            if (zoom) style.ZoomAtDisplayPoint(zoomFactor, zoomCenterX, zoomCenterY, !drag);
            if (drag)
            {
                if (pan)
                    style.PanByDisplayDelta(dragPixelsX * _panSensitivity, dragPixelsY * _panSensitivity);
                else
                    style.RotateByDisplayDelta(dragPixelsX, dragPixelsY);
            }
        }
    }
}
