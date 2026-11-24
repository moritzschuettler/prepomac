// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using System.IO;
using DynamicTypeDescriptor;
using System.Drawing;
using System.Runtime.Serialization;

namespace PrePoMax
{
    [PmxType(10530)]
    public enum BackgroundType
    {
        [StandardValue("White", Description = "White color background.")]
        White,

        [StandardValue("Solid", Description = "One color background.")]
        Solid,

        [StandardValue("Gradient", DisplayName = "Gradient", Description = "Two color background.")]
        Gradient,
    }

    [PmxType(10531)]
    public class GraphicsSettings : ISettings
    {
        // Variables                                                                                                                
        [PmxKey(0)] private BackgroundType _backgroundType;
        [PmxKey(1)] private Color _topColor;
        [PmxKey(2)] private Color _bottomColor;
        [PmxKey(3)] private bool _coorSysVisibility;
        [PmxKey(4)] private bool _scaleWidgetVisibility;
        [PmxKey(5)] private bool _widgetHighlightEnabled;
        [PmxKey(6)] private double _ambientComponent;
        [PmxKey(7)] private double _diffuseComponent;
        [PmxKey(8)] private bool _pointSmoothing;
        [PmxKey(9)] private bool _lineSmoothing;
        [PmxKey(10)] private bool _linearDeflectionRelative;
        [PmxKey(11)] private double _linearDeflection;
        [PmxKey(12)] private double _angularDeflectionDeg;
        // The choice of the ActiViz (VTK 5.8) view, which settings files of the builds that had it still carry; it is
        // read and never used
        [PmxKey(13)] [OptionalField]
        private bool _activizView;
        // Older settings files have no ambient occlusion, which reads as off
        [PmxKey(14)] [OptionalField]
        private bool _ambientOcclusion;
        [PmxKey(15)] [OptionalField]
        private double _ambientOcclusionRadius;
        [PmxKey(16)] [OptionalField]
        private int _ambientOcclusionKernelSize;
        // Older settings files have no text font size, which reads as the default
        [PmxKey(17)] [OptionalField]
        private int _textFontSize;
        [PmxKey(18)] [OptionalField]
        private string _textFontFamily;


        // Properties                                                                                                               
        public BackgroundType BackgroundType
        {
            get
            {
                return _backgroundType;
            }
            set
            {
                if (value != _backgroundType)
                {
                    _backgroundType = value;
                    if (_backgroundType == BackgroundType.White) _bottomColor = Color.White;
                    else if (_backgroundType == BackgroundType.Solid) _bottomColor = Color.WhiteSmoke;
                    else if (_backgroundType == BackgroundType.Gradient)
                    {
                        _topColor = Color.Gainsboro;
                        _bottomColor = Color.WhiteSmoke;
                    }
                }
            }
        }
        public Color TopColor { get { return _topColor; } set { _topColor = value; } }
        public Color BottomColor { get { return _bottomColor; } set { _bottomColor = value; } }
        public bool CoorSysVisibility { get { return _coorSysVisibility; } set { _coorSysVisibility = value; } }
        public bool ScaleWidgetVisibility { get { return _scaleWidgetVisibility; } set { _scaleWidgetVisibility = value; } }
        public bool WidgetHighlightEnabled
        {
            get { return _widgetHighlightEnabled; }
            set { _widgetHighlightEnabled = value; }
        }
        public string TextFontFamily
        {
            get { return _textFontFamily; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _textFontFamily = vtkControl.Native.VtkNativeViewFont.PreferredFamilyName;
                else _textFontFamily = value.Trim();
            }
        }
        public int TextFontSize
        {
            get { return _textFontSize; }
            set { _textFontSize = vtkControl.Native.VtkNativeViewFont.ClampSize(value); }
        }
        public double AmbientComponent
        {
            get { return _ambientComponent; }
            set
            {
                _ambientComponent = value;
                if (_ambientComponent < 0) _ambientComponent = 0;
                else if (_ambientComponent > 1) _ambientComponent = 1;
            }
        }
        public double DiffuseComponent
        {
            get { return _diffuseComponent; }
            set
            {
                _diffuseComponent = value;
                if (_diffuseComponent < 0) _diffuseComponent = 0;
                else if (_diffuseComponent > 1) _diffuseComponent = 1;
            }
        }
        public bool PointSmoothing { get { return _pointSmoothing; } set { _pointSmoothing = value; } }
        public bool LineSmoothing { get { return _lineSmoothing; } set { _lineSmoothing = value; } }
        public bool LinearDeflectionRelative { get { return _linearDeflectionRelative; } set { _linearDeflectionRelative = value; } }
        public double LinearDeflection
        {
            get { return _linearDeflection; }
            set
            {
                _linearDeflection = value;
                if (_linearDeflection < 0) _linearDeflection = 0;
            }
        }
        public bool AmbientOcclusion { get { return _ambientOcclusion; } set { _ambientOcclusion = value; } }
        public double AmbientOcclusionRadius
        {
            get { return _ambientOcclusionRadius; }
            set
            {
                _ambientOcclusionRadius = value;
                if (_ambientOcclusionRadius < 0.001) _ambientOcclusionRadius = 0.001;
                else if (_ambientOcclusionRadius > 1) _ambientOcclusionRadius = 1;
            }
        }
        public int AmbientOcclusionKernelSize
        {
            get { return _ambientOcclusionKernelSize; }
            set
            {
                _ambientOcclusionKernelSize = value;
                if (_ambientOcclusionKernelSize < 8) _ambientOcclusionKernelSize = 8;
                else if (_ambientOcclusionKernelSize > 512) _ambientOcclusionKernelSize = 512;
            }
        }
        public double AngularDeflectionRad
        {
            get { return Math.Round(_angularDeflectionDeg * Math.PI / 180, 2); }
        }
        public double AngularDeflectionDeg
        {
            get { return _angularDeflectionDeg; }
            set
            {
                _angularDeflectionDeg = value;
                if (_angularDeflectionDeg < 0) _angularDeflectionDeg = 0;
            }
        }


        // Constructors                                                                                                             
        public GraphicsSettings()
        {
            Reset();
        }


        // Methods                                                                                                                  
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _ambientOcclusion = false;
            _ambientOcclusionRadius = 0.05;
            _ambientOcclusionKernelSize = 64;
            _textFontSize = vtkControl.Native.VtkNativeViewFont.DefaultSize;
            _textFontFamily = vtkControl.Native.VtkNativeViewFont.PreferredFamilyName;
        }
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _backgroundType = BackgroundType.Gradient;
            _topColor = Color.Gainsboro;
            _bottomColor = Color.WhiteSmoke;
            _coorSysVisibility = true;
            _scaleWidgetVisibility = true;
            _widgetHighlightEnabled = true;
            _ambientComponent = 0.6;
            _diffuseComponent = 0.6;
            _pointSmoothing = true;
            _lineSmoothing = true;
            _linearDeflectionRelative = true;
            _linearDeflection = 0.01;
            _angularDeflectionDeg = 15;
            _activizView = false;
            _ambientOcclusion = false;
            _ambientOcclusionRadius = 0.05;
            _ambientOcclusionKernelSize = 64;
            _textFontSize = vtkControl.Native.VtkNativeViewFont.DefaultSize;
            _textFontFamily = vtkControl.Native.VtkNativeViewFont.PreferredFamilyName;
        }
    }
}
