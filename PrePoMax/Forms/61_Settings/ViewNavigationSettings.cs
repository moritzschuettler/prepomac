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
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewNavigationSettings : IViewSettings, IReset
    {
        // Variables                                                                                                                
        private NavigationSettings _navigationSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Common                                                                                   
        [CategoryAttribute("Common")]
        [OrderedDisplayName(0, 10, "Rotate sensitivity")]
        [DescriptionAttribute("Set the rotation speed of a mouse drag and of a two finger drag. " +
                              "Values from 0.1 to 10 are allowed.")]
        public double RotateSensitivity
        {
            get { return _navigationSettings.RotateSensitivity; }
            set { _navigationSettings.RotateSensitivity = value; }
        }
        //
        [CategoryAttribute("Common")]
        [OrderedDisplayName(1, 10, "Zoom sensitivity")]
        [DescriptionAttribute("Set the zooming speed of the mouse wheel and of a pinch. " +
                              "Values from 0.1 to 10 are allowed.")]
        public double ZoomSensitivity
        {
            get { return _navigationSettings.ZoomSensitivity; }
            set { _navigationSettings.ZoomSensitivity = value; }
        }
        //
        [CategoryAttribute("Common")]
        [OrderedDisplayName(2, 10, "Zoom direction")]
        [DescriptionAttribute("Reverse the zooming direction of the mouse wheel and of a pinch.")]
        public bool InvertZoom
        {
            get { return _navigationSettings.InvertZoom; }
            set { _navigationSettings.InvertZoom = value; }
        }
        //
        // Trackpad                                                                                 
        [CategoryAttribute("Trackpad")]
        [OrderedDisplayName(0, 10, "Two finger gestures")]
        [DescriptionAttribute("Select when the two finger trackpad gestures are used to rotate, pan and zoom " +
                              "the model. The gestures are only taken while the pointer is over the 3D view.")]
        public TrackpadGesturesEnum TrackpadGestures
        {
            get { return _navigationSettings.TrackpadGestures; }
            set
            {
                _navigationSettings.TrackpadGestures = value;
                UpdateVisibilities();
            }
        }
        //
        [CategoryAttribute("Trackpad")]
        [OrderedDisplayName(1, 10, "Two finger drag")]
        [DescriptionAttribute("Select the action of a two finger drag. The other action is performed while " +
                              "the Shift key is pressed.")]
        public TwoFingerDragEnum TwoFingerDrag
        {
            get { return _navigationSettings.TwoFingerDrag; }
            set { _navigationSettings.TwoFingerDrag = value; }
        }
        //
        [CategoryAttribute("Trackpad")]
        [OrderedDisplayName(2, 10, "Pan sensitivity")]
        [DescriptionAttribute("Set the panning speed of a two finger drag. Values from 0.1 to 10 are allowed. " +
                              "The mouse pans the model directly and is not affected.")]
        public double PanSensitivity
        {
            get { return _navigationSettings.PanSensitivity; }
            set { _navigationSettings.PanSensitivity = value; }
        }
        //
        [CategoryAttribute("Trackpad")]
        [OrderedDisplayName(3, 10, "Left - right direction")]
        [DescriptionAttribute("Reverse the direction of the horizontal component of a two finger drag.")]
        public bool InvertX
        {
            get { return _navigationSettings.InvertX; }
            set { _navigationSettings.InvertX = value; }
        }
        //
        [CategoryAttribute("Trackpad")]
        [OrderedDisplayName(4, 10, "Up - down direction")]
        [DescriptionAttribute("Reverse the direction of the vertical component of a two finger drag.")]
        public bool InvertY
        {
            get { return _navigationSettings.InvertY; }
            set { _navigationSettings.InvertY = value; }
        }


        // Constructors                                                                               
        public ViewNavigationSettings(NavigationSettings navigationSettings)
        {
            _navigationSettings = navigationSettings;
            _dctd = ProviderInstaller.Install(this);
            //
            _dctd.RenameBooleanProperty(nameof(InvertZoom), "Reversed", "Normal");
            _dctd.RenameBooleanProperty(nameof(InvertX), "Reversed", "Normal");
            _dctd.RenameBooleanProperty(nameof(InvertY), "Reversed", "Normal");
            //
            UpdateVisibilities();
        }


        // Methods                                                                               
        public ISettings GetBase()
        {
            return _navigationSettings;
        }
        public void Reset()
        {
            _navigationSettings.Reset();
            //
            UpdateVisibilities();
        }
        public void UpdateVisibilities()
        {
            bool visible = _navigationSettings.GesturesEnabled;
            _dctd.GetProperty(nameof(TwoFingerDrag)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(PanSensitivity)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(InvertX)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(InvertY)).SetIsBrowsable(visible);
        }
    }
}
