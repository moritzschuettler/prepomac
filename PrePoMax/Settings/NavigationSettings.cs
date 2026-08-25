// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public enum TrackpadGesturesEnum
    {
        [StandardValue("Off", Description = "Two finger gestures are ignored.")]
        Off,

        [StandardValue("Automatic", Description = "Two finger gestures are used when a precision touchpad " +
                                                  "is present.")]
        Automatic,

        [StandardValue("On", Description = "Two finger gestures are used, falling back to touchpad scrolling " +
                                           "when no precision touchpad is present. The mouse wheel then " +
                                           "rotates instead of zooming.")]
        On,
    }

    [Serializable]
    public enum TwoFingerDragEnum
    {
        [StandardValue("Rotate", Description = "A two finger drag rotates and Shift pans.")]
        Rotate,

        [StandardValue("Pan", Description = "A two finger drag pans and Shift rotates.")]
        Pan,
    }

    [Serializable]
    public class NavigationSettings : ISettings
    {
        // Variables                                                                                                                
        private TrackpadGesturesEnum _trackpadGestures;
        private TwoFingerDragEnum _twoFingerDrag;
        private double _rotateSensitivity;
        private double _panSensitivity;
        private double _zoomSensitivity;
        private bool _invertX;
        private bool _invertY;
        private bool _invertZoom;
        private bool _smoothZoom;


        // Properties                                                                                                               
        public TrackpadGesturesEnum TrackpadGestures
        {
            get { return _trackpadGestures; }
            set { _trackpadGestures = value; }
        }
        public TwoFingerDragEnum TwoFingerDrag { get { return _twoFingerDrag; } set { _twoFingerDrag = value; } }
        public double RotateSensitivity
        {
            get { return LimitSensitivity(_rotateSensitivity); }
            set { _rotateSensitivity = LimitSensitivity(value); }
        }
        public double PanSensitivity
        {
            get { return LimitSensitivity(_panSensitivity); }
            set { _panSensitivity = LimitSensitivity(value); }
        }
        public double ZoomSensitivity
        {
            get { return LimitSensitivity(_zoomSensitivity); }
            set { _zoomSensitivity = LimitSensitivity(value); }
        }
        public bool InvertX { get { return _invertX; } set { _invertX = value; } }
        public bool InvertY { get { return _invertY; } set { _invertY = value; } }
        public bool InvertZoom { get { return _invertZoom; } set { _invertZoom = value; } }
        public bool SmoothZoom { get { return _smoothZoom; } set { _smoothZoom = value; } }
        //
        public bool GesturesEnabled { get { return _trackpadGestures != TrackpadGesturesEnum.Off; } }
        public bool UseWheelFallback { get { return _trackpadGestures == TrackpadGesturesEnum.On; } }


        // Constructors                                                                                                             
        public NavigationSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void CheckValues()
        {
            RotateSensitivity = _rotateSensitivity;
            PanSensitivity = _panSensitivity;
            ZoomSensitivity = _zoomSensitivity;
        }
        public void Reset()
        {
            _trackpadGestures = TrackpadGesturesEnum.Automatic;
            _twoFingerDrag = TwoFingerDragEnum.Rotate;
            _rotateSensitivity = 1;
            _panSensitivity = 1;
            _zoomSensitivity = 1;
            _invertX = false;
            _invertY = false;
            _invertZoom = false;
            _smoothZoom = true;
        }
        private static double LimitSensitivity(double value)
        {
            if (value < 0.1) return 0.1;
            else if (value > 10) return 10;
            else return value;
        }
    }
}
