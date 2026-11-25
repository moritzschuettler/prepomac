// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using vtkControl.Native;

namespace vtkControl
{
    // The native scale bar measures the camera and makes its labels and its geometry itself
    class vtkMaxScaleWidget : vtkMaxBorderWidget
    {
        // Variables
        private int _width;
        private int _padding;
        private string _labelFormat;
        private string _unit;
        private VtkNativeTextStyle _textStyle;


        // Constructors
        public vtkMaxScaleWidget()
        {
            _width = 300;
            _padding = 5;
            _labelFormat = "G4";
            _unit = "";
            _textStyle = VtkNativeTextStyle.CreateWidgetStyle();
        }


        // Public setters
        public void SetUnit(string unit)
        {
            _unit = unit;
            _nativeMirror?.SetScaleBarUnit(unit);
        }
        public void SetWidth(int width)
        {
            _width = width;
            _nativeMirror?.SetScaleBarWidth(width);
        }
        public void SetLabelFormat(string labelFormat)
        {
            if (_labelFormat != labelFormat)
            {
                _labelFormat = labelFormat;
                _nativeMirror?.SetScaleBarNumberFormat(labelFormat);
            }
        }
        public void SetTextStyle(VtkNativeTextStyle textStyle)
        {
            _textStyle = textStyle;
            _nativeMirror?.SetTextStyle(textStyle);
        }
        public void SetPadding(int padding)
        {
            if (padding != _padding)
            {
                _padding = padding;
                _nativeMirror?.SetPadding(padding);
            }
        }
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            nativeMirror.SetTextStyle(_textStyle);
            nativeMirror.SetPadding(_padding);
            nativeMirror.SetScaleBarNumberFormat(_labelFormat);
            nativeMirror.SetScaleBarWidth(_width);
            nativeMirror.SetScaleBarUnit(_unit);
            base.CopyStateToNativeMirror(nativeMirror);
        }


        // Public getters
        public string GetLabelFormat()
        {
            return _labelFormat;
        }
    }
}
