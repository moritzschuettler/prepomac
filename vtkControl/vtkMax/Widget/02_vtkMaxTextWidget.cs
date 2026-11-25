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
    class vtkMaxTextWidget : vtkMaxBorderWidget
    {
        // Variables
        protected VtkNativeTextStyle _textStyle;
        protected string _text;
        protected int _padding;
        protected string _numberFormat;
        // The status block sends its lines to the native widget instead of a text
        protected bool _mirrorText = true;


        // Constructors
        public vtkMaxTextWidget()
        {
            _textStyle = VtkNativeTextStyle.CreateWidgetStyle();
            _text = "";
            _padding = 0;
        }


        // Public setters
        public void SetTextStyle(VtkNativeTextStyle textStyle)
        {
            _textStyle = textStyle;
            _nativeMirror?.SetTextStyle(textStyle);
        }
        public virtual void SetText(string text)
        {
            _text = text ?? "";
            if (_mirrorText) _nativeMirror?.SetText(_text);
        }
        public void SetPadding(int padding)
        {
            _padding = padding;
            _nativeMirror?.SetPadding(padding);
        }
        public void SetNumberFormat(string numberFormat)
        {
            _numberFormat = numberFormat;
        }
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            nativeMirror.SetTextStyle(_textStyle);
            nativeMirror.SetPadding(_padding);
            if (_mirrorText) nativeMirror.SetText(_text);
            base.CopyStateToNativeMirror(nativeMirror);
        }


        // Public getters
        public string GetText()
        {
            return _text;
        }
        // Where the first line of the text is drawn, in display coordinates: VtkNativeWidget.GetTextOrigin
        public bool TryGetTextOrigin(out System.Drawing.Point origin)
        {
            origin = System.Drawing.Point.Empty;
            return _nativeMirror != null && _nativeMirror.GetTextOrigin(out origin);
        }
        public string GetNumberFormat()
        {
            return _numberFormat;
        }
    }
}
