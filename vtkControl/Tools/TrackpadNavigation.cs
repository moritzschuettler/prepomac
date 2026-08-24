// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;


namespace vtkControl
{
    public class TrackpadNavigation
    {
        // Variables
        public const double WheelDeltaPerNotch = 120.0;
        private const double _pixelsPerNotch = 40.0;
        private const double _zoomPerNotch = 1.15;
        private const double _pixelsPerMm = 80.0;
        //
        private bool _enabled;
        private bool _twoFingerDragPans;
        private bool _invertZoom;
        private bool _invertX;
        private bool _invertY;
        private double _orbitSensitivity;
        private double _panSensitivity;
        private double _zoomSensitivity;


        // Properties
        public bool Enabled { get { return _enabled; } set { _enabled = value; } }
        public bool TwoFingerDragPans { get { return _twoFingerDragPans; } set { _twoFingerDragPans = value; } }
        public bool InvertZoom { get { return _invertZoom; } set { _invertZoom = value; } }
        public bool InvertX { get { return _invertX; } set { _invertX = value; } }
        public bool InvertY { get { return _invertY; } set { _invertY = value; } }
        public double OrbitSensitivity { get { return _orbitSensitivity; } set { _orbitSensitivity = value; } }
        public double PanSensitivity { get { return _panSensitivity; } set { _panSensitivity = value; } }
        public double ZoomSensitivity { get { return _zoomSensitivity; } set { _zoomSensitivity = value; } }


        // Constructors
        public TrackpadNavigation()
        {
            _enabled = GetEnvironmentFlag("PREPOMAX_TRACKPAD", false);
            _enabled = true;
            _twoFingerDragPans = GetEnvironmentFlag("PREPOMAX_TRACKPAD_DRAG_PANS", false);
            _invertZoom = GetEnvironmentFlag("PREPOMAX_TRACKPAD_INVERT_ZOOM", false);
            _invertX = GetEnvironmentFlag("PREPOMAX_TRACKPAD_INVERT_X", false);
            _invertY = GetEnvironmentFlag("PREPOMAX_TRACKPAD_INVERT_Y", false);
            _orbitSensitivity = GetEnvironmentValue("PREPOMAX_TRACKPAD_ORBIT", 1);
            _panSensitivity = GetEnvironmentValue("PREPOMAX_TRACKPAD_PAN", 1);
            _zoomSensitivity = GetEnvironmentValue("PREPOMAX_TRACKPAD_ZOOM", 1);
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
            if (zoom)
            {
                double factor = Math.Pow(zoomFactor, _zoomSensitivity);
                if (_invertZoom) factor = 1 / factor;
                style.ZoomAtDisplayPoint(factor, zoomCenterX, zoomCenterY, !drag);
            }
            if (drag)
            {
                if (pan)
                    style.PanByDisplayDelta(dragPixelsX * _panSensitivity, dragPixelsY * _panSensitivity);
                else
                    style.RotateByDisplayDelta(dragPixelsX * _orbitSensitivity, dragPixelsY * _orbitSensitivity);
            }
        }
        private static bool GetEnvironmentFlag(string name, bool defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (value == null) return defaultValue;
            value = value.Trim();
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        private static double GetEnvironmentValue(string name, double defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            double parsed;
            if (value != null && double.TryParse(value.Trim(), NumberStyles.Float,
                                                 CultureInfo.InvariantCulture, out parsed)) return parsed;
            return defaultValue;
        }
    }
}
