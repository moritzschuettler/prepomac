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
    enum vtkMaxCoorSysTarget
    {
        None,
        Origin,
        XPositive,
        XNegative,
        YPositive,
        YNegative,
        ZPositive,
        ZNegative,
        XPositiveVertical,
        XNegativeVertical,
        YPositiveVertical,
        YNegativeVertical,
        ZPositiveVertical,
        ZNegativeVertical
    }

    // The triad of the global axes: the native widget follows the camera, finds the click targets and shows the hand
    // cursor over them; its clicks reach the control through the native widget
    class vtkMaxCoorSysWidget : vtkMaxBorderWidget
    {
        // Variables
        private VtkNativeTextStyle _textStyle;
        private int _side;


        // Constructors
        public vtkMaxCoorSysWidget()
        {
            vtkMaxCoorSysGeometry geometry = new vtkMaxCoorSysGeometry();
            _side = (int)Math.Round(2 * (geometry.ShaftLength + geometry.ConeRadius + 2), MidpointRounding.AwayFromZero);
            _textStyle = VtkNativeTextStyle.CreateWidgetStyle();
        }


        // Public methods
        public void UpdateGeometry()
        {
            _nativeMirror?.UpdateTriadGeometry();
        }


        // Public setters
        public void SetTextStyle(VtkNativeTextStyle textStyle)
        {
            _textStyle = textStyle;
            _nativeMirror?.SetTextStyle(textStyle);
        }
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            nativeMirror.SetTextStyle(_textStyle);
            base.CopyStateToNativeMirror(nativeMirror);
        }


        // Public getters
        // The side of the square the triad is drawn in, which does not change with the view
        public override int GetWidth()
        {
            return _side;
        }
    }
}
