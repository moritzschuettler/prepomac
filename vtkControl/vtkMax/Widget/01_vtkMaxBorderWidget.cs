// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using vtkControl.Native;

namespace vtkControl
{
    // The state of a widget of the native view: this class keeps what the control set and hands it to the native
    // widget, which lays the widget out, draws it and takes the mouse. The native widget exists with the widgets area
    // of a native view; it receives the kept state when it is attached and from then on every call. The size and the
    // position a user dragged the widget to are read from it
    class vtkMaxBorderWidget
    {
        // Variables
        protected VtkNativeWidget _nativeMirror;
        //
        protected bool _visibility;
        protected bool _borderVisibility;
        protected bool _backgroundVisibility;
        protected double[] _borderColor;
        protected double[] _backgroundColor;
        protected VtkNativeWidgetAnchor _anchor;
        protected double[] _position;


        // Constructors
        public vtkMaxBorderWidget()
        {
            _visibility = true;
            _borderVisibility = true;
            _backgroundVisibility = true;
            _borderColor = new double[] { 0, 0, 0 };
            _backgroundColor = new double[] { 1, 1, 1 };
            _anchor = VtkNativeWidgetAnchor.BottomLeft;
            _position = new double[] { 100, 100 };
        }


        // Public methods
        public void VisibilityOn()
        {
            _visibility = true;
            _nativeMirror?.SetVisibility(true);
        }
        public void VisibilityOff()
        {
            _visibility = false;
            _nativeMirror?.SetVisibility(false);
        }
        public void BorderVisibilityOn()
        {
            _borderVisibility = true;
            MirrorFrame(_nativeMirror);
        }
        public void BorderVisibilityOff()
        {
            _borderVisibility = false;
            MirrorFrame(_nativeMirror);
        }
        public void BackgroundVisibilityOn()
        {
            _backgroundVisibility = true;
            MirrorFrame(_nativeMirror);
        }
        public void BackgroundVisibilityOff()
        {
            _backgroundVisibility = false;
            MirrorFrame(_nativeMirror);
        }


        // Public setters
        public void SetPosition(double x, double y)
        {
            SetPosition(VtkNativeWidgetAnchor.BottomLeft, x, y);
        }
        public void SetTopLeftPosition(int x, int y)
        {
            SetPosition(VtkNativeWidgetAnchor.TopLeft, x, y);
        }
        public void SetHorizontallyRelativePosition(double x, double y)
        {
            SetPosition(VtkNativeWidgetAnchor.HorizontallyRelative, x, y);
        }
        public void SetBorderColor(double r, double g, double b)
        {
            _borderColor = new double[] { r, g, b };
            MirrorFrame(_nativeMirror);
        }
        public void SetBackgroundColor(double r, double g, double b)
        {
            _backgroundColor = new double[] { r, g, b };
            MirrorFrame(_nativeMirror);
        }
        // The native widget of this one: it receives the kept state, in the order the control sets a widget up, and
        // from then on every call this widget takes. A null widget stops the forwarding
        internal void SetNativeMirror(VtkNativeWidget nativeMirror)
        {
            _nativeMirror = null;
            if (nativeMirror != null) CopyStateToNativeMirror(nativeMirror);
            _nativeMirror = nativeMirror;
        }
        protected virtual void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            MirrorFrame(nativeMirror);
            nativeMirror.SetPosition(_anchor, _position[0], _position[1]);
            nativeMirror.SetVisibility(_visibility);
        }
        protected void MirrorFrame(VtkNativeWidget nativeMirror)
        {
            if (nativeMirror == null) return;
            nativeMirror.SetFrame(_backgroundVisibility, _backgroundColor, _borderVisibility, _borderColor);
        }
        private void SetPosition(VtkNativeWidgetAnchor anchor, double x, double y)
        {
            _anchor = anchor;
            _position[0] = x;
            _position[1] = y;
            _nativeMirror?.SetPosition(anchor, x, y);
        }


        // Public getters
        public int GetVisibility()
        {
            return _visibility ? 1 : 0;
        }
        public int GetBorderVisibility()
        {
            return _borderVisibility ? 1 : 0;
        }
        public int GetBackgroundVisibility()
        {
            return _backgroundVisibility ? 1 : 0;
        }
        // The position measured from the anchor corner and the size: the native widget's, which a drag moves
        public CaeMesh.BoundingBox GetBoundingBox()
        {
            double[] positionAndSize = GetPositionAndSize();
            CaeMesh.BoundingBox box = new CaeMesh.BoundingBox();
            box.MinX = positionAndSize[0];
            box.MaxX = positionAndSize[0] + positionAndSize[2];
            box.MinY = positionAndSize[1];
            box.MaxY = positionAndSize[1] + positionAndSize[3];
            box.MinZ = 0;
            box.MaxZ = 1;
            return box;
        }
        public virtual int GetWidth()
        {
            return (int)Math.Round(GetPositionAndSize()[2], MidpointRounding.AwayFromZero);
        }
        public int GetHeight()
        {
            return (int)Math.Round(GetPositionAndSize()[3], MidpointRounding.AwayFromZero);
        }
        public Rectangle GetRectangle()
        {
            if (_nativeMirror != null) return _nativeMirror.GetRectangle();
            return new Rectangle((int)Math.Round(_position[0], MidpointRounding.AwayFromZero),
                                 (int)Math.Round(_position[1], MidpointRounding.AwayFromZero), 0, 0);
        }
        // Without a native widget the widget has no size yet
        protected double[] GetPositionAndSize()
        {
            if (_nativeMirror != null) return _nativeMirror.GetPositionAndSize(out _);
            return new double[] { _position[0], _position[1], 0, 0 };
        }
    }
}
