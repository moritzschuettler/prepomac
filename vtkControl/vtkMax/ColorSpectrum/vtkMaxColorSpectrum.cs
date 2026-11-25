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
using CaeGlobals;

namespace vtkControl
{
    [PmxType(6107)]
    public class vtkMaxColorSpectrum
    {
        public const int SmoothNumberOfColors = 256;

        // Variables                                                                                                                
        [PmxKey(0)] private vtkColorSpectrumMode _mode;
        [PmxKey(1)] private vtkColorSpectrumType _type;
        [PmxKey(2)] private vtkColorSpectrumMinMaxType _minMaxType;
        [PmxKey(3)] private double _minUserValue;
        [PmxKey(4)] private double _maxUserValue;
        [PmxKey(5)] private System.Drawing.Color _minColor;
        [PmxKey(6)] private System.Drawing.Color _maxColor;
        [PmxKey(7)] private bool _minColorVisible;
        [PmxKey(8)] private bool _maxColorVisible;
        [PmxKey(9)] private int _numberOfColors;
        [PmxKey(10)] private bool _reverseColors;
        [PmxKey(11)] private double _colorBrightness;


        // Properties                                                                                                               
        public vtkColorSpectrumMode Mode { get { return _mode; } set { _mode = value; } }
        public vtkColorSpectrumType Type { get { return _type; } set { _type = value; } }
        public vtkColorSpectrumMinMaxType MinMaxType { get { return _minMaxType; } set { _minMaxType = value; } }
        public bool MinColorVisible { get { return _minColorVisible; } set { _minColorVisible = value; } }
        public bool MaxColorVisible { get { return _maxColorVisible; } set { _maxColorVisible = value; } }
        public double MinUserValue
        {
            get { return _minUserValue; }
            set
            {
                if (value < _maxUserValue) _minUserValue = value;
                else throw new Exception("The min value must be smaller than the max value.");
            }
        }
        public double MaxUserValue
        {
            get { return _maxUserValue; }
            set
            {
                if (value > _minUserValue) _maxUserValue = value;
                else throw new Exception("The max value must be larger than the min value.");
            }
        }
        public System.Drawing.Color MinColor { get { return _minColor; } set { _minColor = value; } }
        public System.Drawing.Color MaxColor { get { return _maxColor; } set { _maxColor = value; } }
        public int NumberOfColors
        {
            get { return _numberOfColors; }
            set
            {
                _numberOfColors = value;
                if (_numberOfColors < 2) _numberOfColors = 2;
                if (_numberOfColors > 24) _numberOfColors = 24;
            }
        }
        public bool ReverseColors { get { return _reverseColors; } set { _reverseColors = value; } }
        public double ColorBrightness
        {
            get { return _colorBrightness; }
            set
            {
                _colorBrightness = value;
                if (_colorBrightness < 0) _colorBrightness = 0;
                else if (_colorBrightness > 1) _colorBrightness = 1;
            }
        }


        // Constructors                                                                                                             
        public vtkMaxColorSpectrum()
        {
            _mode = vtkColorSpectrumMode.Discrete;
            _type = vtkColorSpectrumType.Rainbow;
            _minMaxType = vtkColorSpectrumMinMaxType.Automatic;
            _minColorVisible = true;
            _maxColorVisible = true;
            _minUserValue = 0;
            _maxUserValue = 1;
            _minColor = System.Drawing.Color.LightGray;
            _maxColor = System.Drawing.Color.DarkGray;
            _numberOfColors = 9;
            _reverseColors = false;
            _colorBrightness = 0.2;
        }


        // Methods                                                                                                                  
        public void DeepCopy(vtkMaxColorSpectrum source)
        {
            _mode = source.Mode;
            _type = source.Type;
            _minMaxType = source.MinMaxType;
            _minUserValue = source.MinUserValue;
            _maxUserValue = source.MaxUserValue;
            _minColor = source.MinColor;
            _maxColor = source.MaxColor;
            _numberOfColors = source.NumberOfColors;
            _reverseColors = source._reverseColors;
            _colorBrightness = source._colorBrightness;
        }
        public void SetMinMax(vtkMaxColorSpectrum colorSpectrum)
        {
            _minMaxType = colorSpectrum._minMaxType;
            _minUserValue = colorSpectrum._minUserValue;
            _maxUserValue = colorSpectrum._maxUserValue;
            _minColor = colorSpectrum._minColor;
            _maxColor = colorSpectrum._maxColor;
        }
        public void SetUserMinMax(double minUserValue, double maxUserValue)
        {
            if (double.IsNaN(minUserValue) || double.IsInfinity(minUserValue) ||
                double.IsNaN(maxUserValue) || double.IsInfinity(maxUserValue) ||
                minUserValue >= maxUserValue)
                throw new Exception("The min value must be smaller than the max value.");
            //
            _minUserValue = minUserValue;
            _maxUserValue = maxUserValue;
        }
        // The color of the spectrum at a position from 0 to 1 as the result coloring draws it
        public System.Drawing.Color GetColor(double position)
        {
            double[] rgb = Native.VtkNativeColorSpectrum.FromColorSpectrum(this).GetColor(position);
            return System.Drawing.Color.FromArgb(255, (int)(rgb[0] * 255), (int)(rgb[1] * 255), (int)(rgb[2] * 255));
        }
        public bool IsMinMaxEqual(vtkMaxColorSpectrum colorSpectrum)
        {
            if (_minMaxType != colorSpectrum._minMaxType) return false;
            else if (_minUserValue != colorSpectrum._minUserValue) return false;
            else if (_maxUserValue != colorSpectrum._maxUserValue) return false;
            else if (_minColor != colorSpectrum._minColor) return false;
            else if (_maxColor != colorSpectrum._maxColor) return false;

            return false;
        }

    }
}
