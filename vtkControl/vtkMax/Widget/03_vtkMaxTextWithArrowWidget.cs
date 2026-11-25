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
    // The native widget places the box from the anchor, keeps it in the plane of the anchor while the camera moves and
    // draws the leader; its box and leader are what the arrangement of the boxes reads
    class vtkMaxTextWithArrowWidget : vtkMaxTextWidget
    {
        // Variables
        private const double AnchorPointBoxOffset = 15;
        //
        private string _name;
        private double[] _anchorPoint;


        // Constructors
        public vtkMaxTextWithArrowWidget(string name)
        {
            _name = name;
            _anchorPoint = null;
        }


        // Public methods
        public void ResetInitialPosition()
        {
            _nativeMirror?.ResetInitialPosition();
        }
        public void ResetPositionFromAnchor()
        {
            if (_anchorPoint != null) _nativeMirror?.ResetPositionFromAnchor();
        }
        public void OffsetPosition(double x, double y)
        {
            _nativeMirror?.OffsetPosition(x, y);
        }


        // Public setters
        public void SetAnchorPoint(double x, double y, double z)
        {
            _anchorPoint = new double[] { x, y, z };
            _nativeMirror?.SetAnchorPoint(x, y, z);
        }
        // The native box takes the anchor, from which it places itself, instead of a position, before it is shown
        protected override void CopyStateToNativeMirror(VtkNativeWidget nativeMirror)
        {
            if (_anchorPoint == null)
            {
                base.CopyStateToNativeMirror(nativeMirror);
                return;
            }
            nativeMirror.SetTextStyle(_textStyle);
            nativeMirror.SetPadding(_padding);
            nativeMirror.SetText(_text);
            MirrorFrame(nativeMirror);
            nativeMirror.SetAnchorPoint(_anchorPoint[0], _anchorPoint[1], _anchorPoint[2]);
            nativeMirror.SetVisibility(_visibility);
        }


        // Public getters
        public string GetName()
        {
            return _name;
        }
        public CaeMesh.BoundingBox GetAnchorPointBox()
        {
            double[] head = GetLeaderLine()[1];
            //
            CaeMesh.BoundingBox box = new CaeMesh.BoundingBox();
            box.MinX = head[0] - AnchorPointBoxOffset;
            box.MaxX = head[0] + AnchorPointBoxOffset;
            box.MinY = head[1] - AnchorPointBoxOffset;
            box.MaxY = head[1] + AnchorPointBoxOffset;
            box.MinZ = 0;
            box.MaxZ = 1;
            //
            return box;
        }
        // The leader from the box to the arrow head: { start, end }, each x, y
        public double[][] GetLeaderLine()
        {
            if (_nativeMirror != null) return _nativeMirror.GetLeaderLine();
            return new double[][] { new double[] { _position[0], _position[1] }, new double[] { 0, 0 } };
        }
    }
}
