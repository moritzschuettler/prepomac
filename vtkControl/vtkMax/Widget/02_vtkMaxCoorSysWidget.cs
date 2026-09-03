// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kitware.VTK;

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
        ZNegative
    }

    class vtkMaxCoorSysWidget : vtkMaxBorderWidget
    {
        // Variables                                                                                                                
        private const int NumOfAxes = vtkMaxCoorSysGeometry.NumOfAxes;
        private const int NumOfEnds = vtkMaxCoorSysGeometry.NumOfEnds;
        //
        private vtkMaxCoorSysGeometry _geometry;
        private double _positiveHitRadius;
        private double _negativeHitRadius;
        private double _originHitRadius;
        private double _boundsPadding;
        private int[][] _labelSizes;
        private uint _cameraModifiedTime;
        private bool _geometryGenerated;
        private bool _hovered;
        private vtkMaxCoorSysTarget _pressedTarget;
        //
        protected vtkTextMapper[] _labelMappers;
        protected vtkActor2D[] _labelActors;
        //
        protected vtkPolyDataMapper2D _shaftMapper;
        protected vtkActor2D _shaftActor;
        //
        protected vtkPolyDataMapper2D _endMapper;
        protected vtkActor2D _endActor;


        // Events                                                                                                                   
        public event Action<vtkMaxCoorSysTarget> TargetClicked;
        public event Action<bool> HoverChanged;


        // Constructors                                                                                                             
        public vtkMaxCoorSysWidget()
        {
            _geometry = new vtkMaxCoorSysGeometry();
            _positiveHitRadius = 11;
            _negativeHitRadius = 8;
            _originHitRadius = 7;
            _boundsPadding = 5;
            //
            _labelSizes = new int[NumOfAxes][];
            _cameraModifiedTime = 0;
            _geometryGenerated = false;
            _hovered = false;
            _pressedTarget = vtkMaxCoorSysTarget.None;
            //
            double side = 2 * (_geometry.ShaftLength + _geometry.ConeRadius + 2);
            _size[0] = side;
            _size[1] = side;
            //
            InitializeLabels();
            InitializeShafts();
            InitializeEnds();
        }


        // Private methods                                                                                                          
        private void InitializeLabels()
        {
            string[] labels = new string[] { "X", "Y", "Z" };
            vtkTextProperty textProperty = vtkTextProperty.New();
            //
            _labelMappers = new vtkTextMapper[NumOfAxes];
            _labelActors = new vtkActor2D[NumOfAxes];
            //
            for (int i = 0; i < NumOfAxes; i++)
            {
                // Mapper
                _labelMappers[i] = vtkTextMapper.New();
                _labelMappers[i].SetTextProperty(textProperty);
                _labelMappers[i].SetInput(labels[i]);
                // Actor
                _labelActors[i] = vtkActor2D.New();
                _labelActors[i].SetMapper(_labelMappers[i]);
                // Set relative text position
                _labelActors[i].GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
                _labelActors[i].GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
                _labelActors[i].GetPositionCoordinate().SetValue(0, 0);
            }
        }
        private void InitializeShafts()
        {
            vtkPolyData shaftPolyData = vtkPolyData.New();
            shaftPolyData.SetPoints(vtkPoints.New());
            shaftPolyData.SetLines(vtkCellArray.New());
            //
            _shaftMapper = vtkPolyDataMapper2D.New();
            _shaftMapper.SetInput(shaftPolyData);
            _shaftMapper.SetScalarModeToUseCellData();
            _shaftMapper.SetColorModeToDefault();
            _shaftMapper.ScalarVisibilityOn();
            //
            _shaftActor = vtkActor2D.New();
            _shaftActor.SetMapper(_shaftMapper);
            _shaftActor.GetProperty().SetLineWidth(2f);
            //
            _shaftActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
            _shaftActor.GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
        }
        private void InitializeEnds()
        {
            vtkPolyData endPolyData = vtkPolyData.New();
            endPolyData.SetPoints(vtkPoints.New());
            endPolyData.SetPolys(vtkCellArray.New());
            //
            _endMapper = vtkPolyDataMapper2D.New();
            _endMapper.SetInput(endPolyData);
            _endMapper.SetScalarModeToUseCellData();
            _endMapper.SetColorModeToDefault();
            _endMapper.ScalarVisibilityOn();
            //
            _endActor = vtkActor2D.New();
            _endActor.SetMapper(_endMapper);
            //
            _endActor.GetPositionCoordinate().SetCoordinateSystemToDisplay();  // set offsets in pixels
            _endActor.GetPositionCoordinate().SetReferenceCoordinate(_positionCoordinate);
        }
        //
        private void GenerateGeometry(bool force)
        {
            if (_renderer == null) return;
            // If this function is called when the renderer is not yet loaded, return
            int[] size = _renderer.GetSize();
            if (size[0] == 0 && size[1] == 0) return;
            //
            vtkCamera camera = _renderer.GetActiveCamera();
            uint cameraModifiedTime = camera == null ? 0 : camera.GetMTime();
            if (!force && _geometryGenerated && cameraModifiedTime == _cameraModifiedTime) return;
            _cameraModifiedTime = cameraModifiedTime;
            //
            ComputeEndProjections(camera);    // must be before all the rest
            GenerateShafts();
            GenerateEnds();
            PositionLabels();
            //
            _geometryGenerated = true;
        }
        private void ComputeEndProjections(vtkCamera camera)
        {
            double[] right;
            double[] up;
            double[] viewPlaneNormal;
            vtkMaxCoorSysGeometry.GetCameraBasis(camera, out right, out up, out viewPlaneNormal);
            // The widget always shows the global axes at the center of its own display rectangle
            _geometry.Update(GetLocalCenter(), 0, vtkMaxCoorSysGeometry.GlobalAxes, right, up, viewPlaneNormal);
        }
        private void GenerateShafts()
        {
            vtkPoints points = vtkPoints.New();
            vtkCellArray lines = vtkCellArray.New();
            vtkUnsignedCharArray colors = vtkUnsignedCharArray.New();
            colors.SetNumberOfComponents(4);
            // The ends closer to the viewer must be drawn over the ends behind them
            foreach (int endId in _geometry.GetEndIdsSortedByDepth())
            {
                if (!_geometry.IsEndVisible(endId)) continue;
                //
                _geometry.AppendShaft(points, lines, colors, endId);
            }
            //
            vtkPolyData shaftPolyData = _shaftMapper.GetInput();
            shaftPolyData.SetPoints(points);
            shaftPolyData.SetLines(lines);
            shaftPolyData.GetCellData().SetScalars(colors);
            shaftPolyData.Modified();
        }
        private void GenerateEnds()
        {
            vtkPoints points = vtkPoints.New();
            vtkCellArray polygons = vtkCellArray.New();
            vtkUnsignedCharArray colors = vtkUnsignedCharArray.New();
            colors.SetNumberOfComponents(4);
            //
            _geometry.AppendOrigin(points, polygons, colors);
            // The ends closer to the viewer must be drawn over the ends behind them
            foreach (int endId in _geometry.GetEndIdsSortedByDepth())
            {
                if (!_geometry.IsEndVisible(endId)) continue;
                //
                _geometry.AppendEnd(points, polygons, colors, endId);
            }
            //
            vtkPolyData endPolyData = _endMapper.GetInput();
            endPolyData.SetPoints(points);
            endPolyData.SetPolys(polygons);
            endPolyData.GetCellData().SetScalars(colors);
            endPolyData.Modified();
        }
        private void PositionLabels()
        {
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                double[] labelPosition = _geometry.GetLabelPosition(axisId, GetLabelSize(axisId));
                _labelActors[axisId].GetPositionCoordinate().SetValue(labelPosition[0], labelPosition[1]);
            }
        }
        //
        private int[] GetLabelSize(int axisId)
        {
            if (_labelSizes[axisId] == null)
                _labelSizes[axisId] = vtkMaxWidgetTools.GetTextSize(_labelMappers[axisId], _renderer);
            //
            return _labelSizes[axisId];
        }
        private int GetMaxLabelSize()
        {
            int maxLabelSize = 0;
            for (int axisId = 0; axisId < NumOfAxes; axisId++)
            {
                int[] labelSize = GetLabelSize(axisId);
                maxLabelSize = Math.Max(maxLabelSize, Math.Max(labelSize[0], labelSize[1]));
            }
            return maxLabelSize;
        }
        private double[] GetLocalCenter()
        {
            return new double[] { _size[0] / 2, _size[1] / 2 };
        }
        private double[] GetDisplayPosition()
        {
            if (!_visibility || _renderer == null || _position == null) return null;
            //
            double[] position = _position.ToArray();
            if (_widgetPosition == vtkMaxWidgetPosition.FromTopLeft)
                position[1] = _renderer.GetSize()[1] - _size[1] - position[1];
            //
            return new double[] { Math.Floor(position[0]), Math.Floor(position[1]) };
        }
        private double GetEndHitRadius(int endId)
        {
            return vtkMaxCoorSysGeometry.IsPositiveEnd(endId) ? _positiveHitRadius : _negativeHitRadius;
        }
        private double[] GetTargetLocalPosition(vtkMaxCoorSysTarget target)
        {
            if (target == vtkMaxCoorSysTarget.Origin) return GetLocalCenter();
            //
            return _geometry.GetEndTargetPosition(GetEndId(target));
        }
        private double GetTargetHitRadius(vtkMaxCoorSysTarget target)
        {
            if (target == vtkMaxCoorSysTarget.Origin) return _originHitRadius;
            //
            return GetEndHitRadius(GetEndId(target));
        }
        //
        private bool SetNegativeAxesVisible(bool visible)
        {
            if (!_geometry.SetNegativeAxesVisible(visible)) return false;
            //
            GenerateGeometry(true);
            //
            return true;
        }
        private static int GetEndId(vtkMaxCoorSysTarget target)
        {
            switch (target)
            {
                case vtkMaxCoorSysTarget.XPositive: return 0;
                case vtkMaxCoorSysTarget.XNegative: return 1;
                case vtkMaxCoorSysTarget.YPositive: return 2;
                case vtkMaxCoorSysTarget.YNegative: return 3;
                case vtkMaxCoorSysTarget.ZPositive: return 4;
                case vtkMaxCoorSysTarget.ZNegative: return 5;
                default: throw new NotSupportedException();
            }
        }
        private static vtkMaxCoorSysTarget GetTarget(int endId)
        {
            switch (endId)
            {
                case 0: return vtkMaxCoorSysTarget.XPositive;
                case 1: return vtkMaxCoorSysTarget.XNegative;
                case 2: return vtkMaxCoorSysTarget.YPositive;
                case 3: return vtkMaxCoorSysTarget.YNegative;
                case 4: return vtkMaxCoorSysTarget.ZPositive;
                case 5: return vtkMaxCoorSysTarget.ZNegative;
                default: throw new NotSupportedException();
            }
        }
        private void SetHovered(bool hovered)
        {
            if (_hovered == hovered) return;
            //
            _hovered = hovered;
            HoverChanged?.Invoke(_hovered);
        }


        // Public methods                                                                                                           
        public vtkMaxCoorSysTarget GetTargetAtPosition(int x, int y)
        {
            if (!_visibility || !_geometryGenerated) return vtkMaxCoorSysTarget.None;
            //
            double[] position = GetDisplayPosition();
            if (position == null) return vtkMaxCoorSysTarget.None;
            // The end closest to the viewer wins when the projections overlap
            int closestEndId = -1;
            for (int endId = 0; endId < NumOfEnds; endId++)
            {
                if (!_geometry.IsEndVisible(endId)) continue;
                //
                double[] endTargetPosition = _geometry.GetEndTargetPosition(endId);
                double hitRadius = GetEndHitRadius(endId);
                double dx = position[0] + endTargetPosition[0] - x;
                double dy = position[1] + endTargetPosition[1] - y;
                if (dx * dx + dy * dy > hitRadius * hitRadius) continue;
                //
                if (closestEndId < 0 || _geometry.GetEndDepth(endId) > _geometry.GetEndDepth(closestEndId))
                    closestEndId = endId;
            }
            if (closestEndId >= 0) return GetTarget(closestEndId);
            //
            double[] center = GetLocalCenter();
            double dxOrigin = position[0] + center[0] - x;
            double dyOrigin = position[1] + center[1] - y;
            if (dxOrigin * dxOrigin + dyOrigin * dyOrigin <= _originHitRadius * _originHitRadius)
                return vtkMaxCoorSysTarget.Origin;
            //
            return vtkMaxCoorSysTarget.None;
        }
        public override void OnRenderWindowModified()
        {
            if (_visibility)
            {
                base.OnRenderWindowModified();
                OnSizeChanged();
            }
        }
        public override void CameraModified()
        {
            if (_visibility)
            {
                base.CameraModified();
                GenerateGeometry(false);
            }
        }
        public override void MouseRotate(double azimuthAngle, double ElevationAngle)
        {
            if (_visibility)
            {
                base.MouseRotate(azimuthAngle, ElevationAngle);
                GenerateGeometry(false);
            }
        }
        public void UpdateGeometry()
        {
            if (_visibility) GenerateGeometry(false);
        }
        public override void OnSizeChanged()
        {
            GenerateGeometry(true);
            OnMovedOrSizeChanged();
        }
        public override bool LeftButtonPress(MouseEventArgs e)
        {
            _pressedTarget = GetTargetAtPosition(e.Location.X, e.Location.Y);
            //
            return _pressedTarget != vtkMaxCoorSysTarget.None;
        }
        public override bool LeftButtonRelease(int x, int y)
        {
            if (_pressedTarget == vtkMaxCoorSysTarget.None) return false;
            //
            vtkMaxCoorSysTarget pressedTarget = _pressedTarget;
            _pressedTarget = vtkMaxCoorSysTarget.None;
            if (GetTargetAtPosition(x, y) == pressedTarget) TargetClicked?.Invoke(pressedTarget);
            //
            return true;
        }
        public override bool RightButtonPress(int x, int y)
        {
            return false;
        }
        public override bool MouseMove(int x, int y)
        {
            return false;
        }
        public override bool MouseOver(int x, int y)
        {
            return GetTargetAtPosition(x, y) != vtkMaxCoorSysTarget.None;
        }
        public override bool UpdateMouseProximity(int x, int y)
        {
            return SetNegativeAxesVisible(GetDisplayRectangle().Contains(x, y));
        }
        public override bool UpdateMouseHover(int x, int y, bool mouseOver, bool highlightEntireWidget = false)
        {
            Rectangle rectangle = mouseOver ? GetHighlightRectangle(x, y) : Rectangle.Empty;
            bool highlightChanged = _highlight.SetRectangle(rectangle, vtkMaxWidgetHighlight.WidgetFillOpacity);
            //
            SetHovered(rectangle != Rectangle.Empty);
            //
            return highlightChanged;
        }
        public override bool ClearMouseProximity()
        {
            return SetNegativeAxesVisible(false);
        }
        public override bool ClearMouseHover()
        {
            SetHovered(false);
            //
            return base.ClearMouseHover();
        }
        public override void VisibilityOn()
        {
            if (_visibility == false)
            {
                base.VisibilityOn();
                //
                if (_shaftActor != null) _renderer.AddActor(_shaftActor);
                if (_endActor != null) _renderer.AddActor(_endActor);
                //
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    if (_labelActors[i] != null) _renderer.AddActor(_labelActors[i]);
                }
            }
        }
        public override void VisibilityOff()
        {
            if (_visibility == true)
            {
                base.VisibilityOff();
                //
                if (_shaftActor != null) _renderer.RemoveActor(_shaftActor);
                if (_endActor != null) _renderer.RemoveActor(_endActor);
                //
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    if (_labelActors[i] != null) _renderer.RemoveActor(_labelActors[i]);
                }
                //
                SetNegativeAxesVisible(false);
                SetHovered(false);
            }
        }
        public override void BringToFront()
        {
            if (!_visibility || _renderer == null) return;
            //
            base.BringToFront();
            if (_shaftActor != null)
            {
                _renderer.RemoveActor(_shaftActor);
                _renderer.AddActor(_shaftActor);
            }
            if (_endActor != null)
            {
                _renderer.RemoveActor(_endActor);
                _renderer.AddActor(_endActor);
            }
            for (int i = 0; i < _labelActors.Length; i++)
            {
                if (_labelActors[i] != null)
                {
                    _renderer.RemoveActor(_labelActors[i]);
                    _renderer.AddActor(_labelActors[i]);
                }
            }
        }


        // Protected methods                                                                                                        
        protected override Rectangle GetDisplayRectangle()
        {
            Rectangle rectangle = base.GetDisplayRectangle();
            if (rectangle == Rectangle.Empty) return rectangle;
            // A square that contains the axis labels in any view orientation
            double[] position = GetDisplayPosition();
            double[] center = GetLocalCenter();
            double halfSide = _geometry.ShaftLength + _geometry.LabelGap + GetMaxLabelSize() + _boundsPadding;
            double centerX = position[0] + center[0];
            double centerY = position[1] + center[1];
            //
            return Rectangle.FromLTRB((int)Math.Floor(centerX - halfSide), (int)Math.Floor(centerY - halfSide),
                                      (int)Math.Ceiling(centerX + halfSide), (int)Math.Ceiling(centerY + halfSide));
        }
        protected override Rectangle GetHighlightRectangle(int x, int y)
        {
            vtkMaxCoorSysTarget target = GetTargetAtPosition(x, y);
            double[] position = GetDisplayPosition();
            if (target == vtkMaxCoorSysTarget.None || position == null) return Rectangle.Empty;
            //
            double[] localPosition = GetTargetLocalPosition(target);
            double radius = GetTargetHitRadius(target);
            double centerX = position[0] + localPosition[0];
            double centerY = position[1] + localPosition[1];
            //
            return Rectangle.FromLTRB((int)Math.Floor(centerX - radius), (int)Math.Floor(centerY - radius),
                                      (int)Math.Ceiling(centerX + radius), (int)Math.Ceiling(centerY + radius));
        }


        // Public setters                                                                                                           
        public override void SetInteractor(vtkRenderer renderer, vtkRenderWindowInteractor renderWindowInteractor)
        {
            base.SetInteractor(renderer, renderWindowInteractor);
            if (renderer == null || renderWindowInteractor == null) return;
            //
            if (_visibility)
            {
                _renderer.AddActor(_shaftActor);
                _renderer.AddActor(_endActor);
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    _renderer.AddActor(_labelActors[i]);
                }
            }
            //
            OnSizeChanged();
        }
        public override void RemoveInteractor()
        {
            if (_renderer != null)
            {
                _renderer.RemoveActor(_shaftActor);
                _renderer.RemoveActor(_endActor);
                for (int i = 0; i < _labelActors.Length; i++)
                {
                    _renderer.RemoveActor(_labelActors[i]);
                }
            }
            //
            base.RemoveInteractor();
        }
        public void SetTextProperty(vtkTextProperty textProperty)
        {
            for (int i = 0; i < _labelMappers.Length; i++)
            {
                _labelMappers[i].SetTextProperty(textProperty);
                _labelSizes[i] = null;
            }
            //
            GenerateGeometry(true);
        }
    }
}
